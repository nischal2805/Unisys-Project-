using ArazzoWorkflowPlatform.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Infrastructure.Services;

/// <summary>
/// Service for chunking OpenAPI documents into manageable pieces for embedding
/// </summary>
public class OpenApiChunkingService : IChunkingService
{
    private readonly ILogger<OpenApiChunkingService> _logger;
    private const int MaxChunkTokens = 500; // Keep chunks small for better retrieval
    private const double CharsPerToken = 4.0; // Rough approximation

    public OpenApiChunkingService(ILogger<OpenApiChunkingService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<RawChunk> ChunkOpenApiDocument(
        string openApiContent, 
        Guid fileId,
        ChunkingStrategy strategy = ChunkingStrategy.Auto)
    {
        var chunks = new List<RawChunk>();
        
        // Determine strategy if auto
        if (strategy == ChunkingStrategy.Auto)
        {
            strategy = DetermineOptimalStrategy(openApiContent);
        }
        
        _logger.LogInformation("[CHUNKING] Using {Strategy} strategy for file {FileId}", strategy, fileId);

        try
        {
            // Try to parse as JSON first
            if (openApiContent.TrimStart().StartsWith("{"))
            {
                var doc = JsonDocument.Parse(openApiContent);
                chunks = strategy switch
                {
                    ChunkingStrategy.ByPath => ChunkByPath(doc, fileId),
                    ChunkingStrategy.ByOperation => ChunkByOperation(doc, fileId),
                    ChunkingStrategy.BySchema => ChunkBySchema(doc, fileId),
                    _ => ChunkByOperation(doc, fileId) // Default
                };
            }
            else
            {
                // YAML or plain text - treat as single chunk or simple split
                _logger.LogDebug("[CHUNKING] Content is not JSON, using text-based chunking");
                chunks = ChunkByText(openApiContent, fileId);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[CHUNKING] Failed to parse as JSON, falling back to text chunking");
            chunks = ChunkByText(openApiContent, fileId);
        }

        // Update total chunks count in metadata
        var totalChunks = chunks.Count;
        for (int i = 0; i < chunks.Count; i++)
        {
            chunks[i] = chunks[i] with
            {
                Metadata = chunks[i].Metadata with
                {
                    ChunkIndex = i,
                    TotalChunks = totalChunks
                }
            };
        }

        _logger.LogInformation("[CHUNKING] Created {ChunkCount} chunks for file {FileId}", chunks.Count, fileId);
        return chunks;
    }

    public int EstimateTokenCount(string text)
    {
        return (int)Math.Ceiling(text.Length / CharsPerToken);
    }

    public bool NeedsChunking(string content, int maxTokens = 4000)
    {
        return EstimateTokenCount(content) > maxTokens;
    }

    private ChunkingStrategy DetermineOptimalStrategy(string content)
    {
        var tokenCount = EstimateTokenCount(content);
        
        if (tokenCount <= 2000)
        {
            // Small document - chunk by operation for granularity
            return ChunkingStrategy.ByOperation;
        }
        else if (tokenCount <= 8000)
        {
            // Medium document - chunk by path
            return ChunkingStrategy.ByPath;
        }
        else
        {
            // Large document - chunk by operation for maximum granularity
            return ChunkingStrategy.ByOperation;
        }
    }

    private List<RawChunk> ChunkByPath(JsonDocument doc, Guid fileId)
    {
        var chunks = new List<RawChunk>();
        
        if (!doc.RootElement.TryGetProperty("paths", out var paths))
        {
            _logger.LogWarning("[CHUNKING] No paths found in OpenAPI document");
            return ChunkByText(doc.RootElement.GetRawText(), fileId);
        }

        foreach (var pathProperty in paths.EnumerateObject())
        {
            var path = pathProperty.Name;
            var pathContent = pathProperty.Value.GetRawText();
            
            // Extract methods for metadata
            var methods = pathProperty.Value.EnumerateObject()
                .Where(p => new[] { "get", "post", "put", "delete", "patch", "options", "head" }.Contains(p.Name.ToLower()))
                .Select(p => p.Name.ToUpper())
                .ToList();

            var chunkContent = $"Path: {path}\n{pathContent}";
            
            // If path content is too large, split by operation
            if (EstimateTokenCount(chunkContent) > MaxChunkTokens)
            {
                chunks.AddRange(ChunkPathByOperations(pathProperty, fileId));
            }
            else
            {
                chunks.Add(new RawChunk
                {
                    Content = chunkContent,
                    Metadata = new ChunkMetadata
                    {
                        ChunkType = "path",
                        Path = path,
                        Method = string.Join(",", methods),
                        Tags = ExtractTags(pathProperty.Value)
                    }
                });
            }
        }

        // Also add schemas as separate chunks if they exist
        if (doc.RootElement.TryGetProperty("components", out var components) &&
            components.TryGetProperty("schemas", out var schemas))
        {
            chunks.AddRange(ChunkSchemas(schemas, fileId));
        }

        return chunks;
    }

    private List<RawChunk> ChunkByOperation(JsonDocument doc, Guid fileId)
    {
        var chunks = new List<RawChunk>();
        
        if (!doc.RootElement.TryGetProperty("paths", out var paths))
        {
            return ChunkByText(doc.RootElement.GetRawText(), fileId);
        }

        foreach (var pathProperty in paths.EnumerateObject())
        {
            chunks.AddRange(ChunkPathByOperations(pathProperty, fileId));
        }

        // Add schemas
        if (doc.RootElement.TryGetProperty("components", out var components) &&
            components.TryGetProperty("schemas", out var schemas))
        {
            chunks.AddRange(ChunkSchemas(schemas, fileId));
        }

        return chunks;
    }

    private List<RawChunk> ChunkPathByOperations(JsonProperty pathProperty, Guid fileId)
    {
        var chunks = new List<RawChunk>();
        var path = pathProperty.Name;
        var httpMethods = new[] { "get", "post", "put", "delete", "patch", "options", "head" };

        foreach (var methodProperty in pathProperty.Value.EnumerateObject())
        {
            if (!httpMethods.Contains(methodProperty.Name.ToLower()))
                continue;

            var method = methodProperty.Name.ToUpper();
            var operationContent = methodProperty.Value.GetRawText();
            
            // Extract operationId if present
            var operationId = methodProperty.Value.TryGetProperty("operationId", out var opIdProp)
                ? opIdProp.GetString()
                : $"{method.ToLower()}{path.Replace("/", "_").Replace("{", "").Replace("}", "")}";

            var summary = methodProperty.Value.TryGetProperty("summary", out var summaryProp)
                ? summaryProp.GetString()
                : null;

            var description = methodProperty.Value.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : null;

            var chunkContent = $"Operation: {method} {path}\n" +
                              $"OperationId: {operationId}\n" +
                              (summary != null ? $"Summary: {summary}\n" : "") +
                              (description != null ? $"Description: {description}\n" : "") +
                              $"Specification:\n{operationContent}";

            chunks.Add(new RawChunk
            {
                Content = chunkContent,
                Metadata = new ChunkMetadata
                {
                    ChunkType = "operation",
                    Path = path,
                    Method = method,
                    OperationId = operationId,
                    Tags = ExtractTags(methodProperty.Value)
                }
            });
        }

        return chunks;
    }

    private List<RawChunk> ChunkBySchema(JsonDocument doc, Guid fileId)
    {
        var chunks = new List<RawChunk>();
        
        // Add paths as a single chunk summary
        if (doc.RootElement.TryGetProperty("paths", out var paths))
        {
            var pathsSummary = "Available API Paths:\n" +
                string.Join("\n", paths.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateObject()
                        .Where(m => new[] { "get", "post", "put", "delete", "patch" }.Contains(m.Name.ToLower()))
                        .Select(m => $"- {m.Name.ToUpper()} {p.Name}")));
            
            chunks.Add(new RawChunk
            {
                Content = pathsSummary,
                Metadata = new ChunkMetadata { ChunkType = "paths_summary" }
            });
        }

        // Add each schema as a chunk
        if (doc.RootElement.TryGetProperty("components", out var components) &&
            components.TryGetProperty("schemas", out var schemas))
        {
            chunks.AddRange(ChunkSchemas(schemas, fileId));
        }

        return chunks;
    }

    private List<RawChunk> ChunkSchemas(JsonElement schemas, Guid fileId)
    {
        var chunks = new List<RawChunk>();

        foreach (var schema in schemas.EnumerateObject())
        {
            var schemaName = schema.Name;
            var schemaContent = schema.Value.GetRawText();
            
            var description = schema.Value.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : null;

            var chunkContent = $"Schema: {schemaName}\n" +
                              (description != null ? $"Description: {description}\n" : "") +
                              $"Definition:\n{schemaContent}";

            // Split large schemas
            if (EstimateTokenCount(chunkContent) > MaxChunkTokens)
            {
                var subChunks = SplitLargeText(chunkContent, schemaName);
                foreach (var subChunk in subChunks)
                {
                    chunks.Add(new RawChunk
                    {
                        Content = subChunk,
                        Metadata = new ChunkMetadata
                        {
                            ChunkType = "schema",
                            SchemaName = schemaName
                        }
                    });
                }
            }
            else
            {
                chunks.Add(new RawChunk
                {
                    Content = chunkContent,
                    Metadata = new ChunkMetadata
                    {
                        ChunkType = "schema",
                        SchemaName = schemaName
                    }
                });
            }
        }

        return chunks;
    }

    private List<RawChunk> ChunkByText(string content, Guid fileId)
    {
        var chunks = new List<RawChunk>();
        var maxChars = (int)(MaxChunkTokens * CharsPerToken);
        
        if (content.Length <= maxChars)
        {
            chunks.Add(new RawChunk
            {
                Content = content,
                Metadata = new ChunkMetadata { ChunkType = "text" }
            });
        }
        else
        {
            // Split by lines while respecting max size
            var lines = content.Split('\n');
            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var line in lines)
            {
                if (currentLength + line.Length > maxChars && currentChunk.Count > 0)
                {
                    chunks.Add(new RawChunk
                    {
                        Content = string.Join("\n", currentChunk),
                        Metadata = new ChunkMetadata { ChunkType = "text" }
                    });
                    currentChunk.Clear();
                    currentLength = 0;
                }
                
                currentChunk.Add(line);
                currentLength += line.Length + 1;
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(new RawChunk
                {
                    Content = string.Join("\n", currentChunk),
                    Metadata = new ChunkMetadata { ChunkType = "text" }
                });
            }
        }

        return chunks;
    }

    private List<string> SplitLargeText(string text, string context)
    {
        var chunks = new List<string>();
        var maxChars = (int)(MaxChunkTokens * CharsPerToken);
        
        for (int i = 0; i < text.Length; i += maxChars)
        {
            var chunk = text.Substring(i, Math.Min(maxChars, text.Length - i));
            chunks.Add($"[Part of {context}]\n{chunk}");
        }

        return chunks;
    }

    private List<string> ExtractTags(JsonElement element)
    {
        var tags = new List<string>();
        
        if (element.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tagsElement.EnumerateArray())
            {
                if (tag.ValueKind == JsonValueKind.String)
                {
                    tags.Add(tag.GetString() ?? "");
                }
            }
        }

        return tags;
    }
}
