using MediatR;

namespace ArazzoWorkflowPlatform.Application.Files.Queries;

/// <summary>
/// Query to retrieve relevant OpenAPI context for a user's question using RAG
/// </summary>
public record GetRelevantContextQuery(
    string Query,
    Guid? FileId = null,
    int MaxResults = 5
) : IRequest<RelevantContextResult>;

public record RelevantContextResult(
    bool Success,
    List<ContextChunk> Chunks,
    string? Error = null
);

public record ContextChunk(
    string Content,
    string ChunkType,
    string? Path,
    string? Method,
    string? OperationId,
    float Score
);
