namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Interface for generating text embeddings
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding for a single text
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    Task<IEnumerable<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the dimension of the embedding vectors
    /// </summary>
    int EmbeddingDimension { get; }
}
