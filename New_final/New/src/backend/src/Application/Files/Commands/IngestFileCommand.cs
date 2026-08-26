using MediatR;

namespace ArazzoWorkflowPlatform.Application.Files.Commands;

/// <summary>
/// Command to ingest an OpenAPI file into the vector store for RAG
/// </summary>
public record IngestFileCommand(Guid FileId) : IRequest<IngestFileResult>;

public record IngestFileResult(
    bool Success,
    int ChunksCreated,
    string? Error = null
);
