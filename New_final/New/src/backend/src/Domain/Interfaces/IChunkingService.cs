using System.Text.Json;

namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Represents different strategies for chunking OpenAPI documents
/// </summary>
public enum ChunkingStrategy
{
    /// <summary>
    /// Chunk by API path - each path becomes a chunk
    /// </summary>
    ByPath,
    
    /// <summary>
    /// Chunk by operation - each HTTP method on a path becomes a chunk
    /// </summary>
    ByOperation,
    
    /// <summary>
    /// Chunk by schema - each schema definition becomes a chunk
    /// </summary>
    BySchema,
    
    /// <summary>
    /// Automatic - intelligently select strategy based on document size
    /// </summary>
    Auto
}

/// <summary>
/// Represents a raw chunk before embedding generation
/// </summary>
public record RawChunk
{
    public required string Content { get; init; }
    public required ChunkMetadata Metadata { get; init; }
}

/// <summary>
/// Interface for chunking OpenAPI documents
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Chunk an OpenAPI document using the specified strategy
    /// </summary>
    IEnumerable<RawChunk> ChunkOpenApiDocument(
        string openApiContent, 
        Guid fileId,
        ChunkingStrategy strategy = ChunkingStrategy.Auto);
    
    /// <summary>
    /// Estimate token count for a text (rough approximation)
    /// </summary>
    int EstimateTokenCount(string text);
    
    /// <summary>
    /// Determine if a document needs chunking based on size
    /// </summary>
    bool NeedsChunking(string content, int maxTokens = 4000);
}
