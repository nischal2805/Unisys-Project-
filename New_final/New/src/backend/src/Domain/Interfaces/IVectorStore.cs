namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Represents a document chunk with its embedding for vector storage
/// </summary>
public record DocumentChunk
{
    public required Guid Id { get; init; }
    public required Guid FileId { get; init; }
    public required string Content { get; init; }
    public required float[] Embedding { get; init; }
    public required ChunkMetadata Metadata { get; init; }
}

/// <summary>
/// Metadata associated with a document chunk
/// </summary>
public record ChunkMetadata
{
    public required string ChunkType { get; init; } // "path", "operation", "schema"
    public string? Path { get; init; }
    public string? Method { get; init; }
    public string? OperationId { get; init; }
    public string? SchemaName { get; init; }
    public List<string> Tags { get; init; } = new();
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
}

/// <summary>
/// Result from a vector search operation
/// </summary>
public record VectorSearchResult
{
    public required Guid ChunkId { get; init; }
    public required Guid FileId { get; init; }
    public required string Content { get; init; }
    public required ChunkMetadata Metadata { get; init; }
    public required float Score { get; init; }
}

/// <summary>
/// Interface for vector database operations
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Initialize the vector store collection if it doesn't exist
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Store a document chunk with its embedding
    /// </summary>
    Task UpsertAsync(DocumentChunk chunk, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Store multiple document chunks in batch
    /// </summary>
    Task UpsertBatchAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search for similar chunks based on query embedding
    /// </summary>
    Task<IEnumerable<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int limit = 5,
        Guid? filterByFileId = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete all chunks associated with a file
    /// </summary>
    Task DeleteByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if chunks exist for a file
    /// </summary>
    Task<bool> HasChunksForFileAsync(Guid fileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the count of chunks for a file
    /// </summary>
    Task<int> GetChunkCountAsync(Guid fileId, CancellationToken cancellationToken = default);
}
