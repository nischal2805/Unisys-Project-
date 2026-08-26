using ArazzoWorkflowPlatform.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Services;

/// <summary>
/// Embedding service using Ollama's local models (nomic-embed-text)
/// </summary>
public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly ILogger<OllamaEmbeddingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _ollamaEndpoint;
    private readonly string _embeddingModel;
    private readonly int _vectorDimension;

    public OllamaEmbeddingService(
        ILogger<OllamaEmbeddingService> logger,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        var vectorDimStr = configuration["Ollama:VectorDimension"];
        _vectorDimension = int.TryParse(vectorDimStr, out var dim) ? dim : 768;
    }

    public int EmbeddingDimension => _vectorDimension;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaEmbeddingRequest
            {
                Model = _embeddingModel,
                Prompt = text
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_ollamaEndpoint}/api/embeddings",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("[EMBEDDING] Ollama API error: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Ollama embedding failed: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                cancellationToken: cancellationToken);

            if (result?.Embedding == null || result.Embedding.Length == 0)
            {
                throw new InvalidOperationException("Ollama returned empty embedding");
            }

            _logger.LogDebug("[EMBEDDING] Generated embedding with {Dimensions} dimensions", 
                result.Embedding.Length);

            return result.Embedding;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[EMBEDDING] Failed to connect to Ollama at {Endpoint}", _ollamaEndpoint);
            throw new InvalidOperationException($"Cannot connect to Ollama: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, 
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        var embeddings = new List<float[]>(textList.Count);
        
        _logger.LogInformation("[EMBEDDING] Generating embeddings for {Count} texts", textList.Count);

        // Process in batches to avoid overwhelming Ollama
        const int batchSize = 10;
        var processed = 0;

        foreach (var batch in textList.Chunk(batchSize))
        {
            // Process batch items sequentially (Ollama doesn't support batch embedding natively)
            foreach (var text in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
                embeddings.Add(embedding);
                processed++;

                if (processed % 10 == 0)
                {
                    _logger.LogDebug("[EMBEDDING] Progress: {Processed}/{Total}", processed, textList.Count);
                }
            }

            // Small delay between batches to avoid rate limiting
            if (processed < textList.Count)
            {
                await Task.Delay(50, cancellationToken);
            }
        }

        _logger.LogInformation("[EMBEDDING] Completed generating {Count} embeddings", embeddings.Count);
        return embeddings;
    }

    /// <summary>
    /// Ensures the embedding model is pulled and ready
    /// </summary>
    public async Task EnsureModelReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[EMBEDDING] Checking if model {Model} is available", _embeddingModel);

            // First check if model exists
            var listResponse = await _httpClient.GetAsync(
                $"{_ollamaEndpoint}/api/tags",
                cancellationToken);

            if (listResponse.IsSuccessStatusCode)
            {
                var tagsJson = await listResponse.Content.ReadAsStringAsync(cancellationToken);
                var tags = JsonDocument.Parse(tagsJson);
                
                if (tags.RootElement.TryGetProperty("models", out var models))
                {
                    foreach (var model in models.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name) && 
                            name.GetString()?.Contains(_embeddingModel) == true)
                        {
                            _logger.LogInformation("[EMBEDDING] Model {Model} is already available", _embeddingModel);
                            return;
                        }
                    }
                }
            }

            // Model not found, try to pull it
            _logger.LogInformation("[EMBEDDING] Pulling model {Model}...", _embeddingModel);
            
            var pullRequest = new { name = _embeddingModel };
            var pullResponse = await _httpClient.PostAsJsonAsync(
                $"{_ollamaEndpoint}/api/pull",
                pullRequest,
                cancellationToken);

            if (!pullResponse.IsSuccessStatusCode)
            {
                var error = await pullResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[EMBEDDING] Could not pull model: {Error}", error);
            }
            else
            {
                _logger.LogInformation("[EMBEDDING] Model {Model} pulled successfully", _embeddingModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[EMBEDDING] Could not verify model availability");
        }
    }

    private class OllamaEmbeddingRequest
    {
        public string Model { get; set; } = "";
        public string Prompt { get; set; } = "";
    }

    private class OllamaEmbeddingResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
