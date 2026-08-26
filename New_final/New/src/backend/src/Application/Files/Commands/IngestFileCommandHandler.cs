using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArazzoWorkflowPlatform.Application.Files.Commands;

/// <summary>
/// Handler for ingesting OpenAPI files into the vector store for RAG-based retrieval
/// </summary>
public class IngestFileCommandHandler : IRequestHandler<IngestFileCommand, IngestFileResult>
{
    private readonly IFileRepository _fileRepository;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<IngestFileCommandHandler> _logger;

    public IngestFileCommandHandler(
        IFileRepository fileRepository,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILogger<IngestFileCommandHandler> logger)
    {
        _fileRepository = fileRepository;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IngestFileResult> Handle(IngestFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[INGEST] Starting ingestion for file {FileId}", request.FileId);

            // 1. Get the file from repository
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                _logger.LogWarning("[INGEST] File not found: {FileId}", request.FileId);
                return new IngestFileResult(false, 0, "File not found");
            }

            _logger.LogInformation("[INGEST] Processing file: {FileName} ({Size} bytes)", 
                file.OriginalName, file.FileSize);

            // 2. Read the file content from disk
            if (!File.Exists(file.FilePath))
            {
                _logger.LogWarning("[INGEST] File not found on disk: {FilePath}", file.FilePath);
                return new IngestFileResult(false, 0, "File not found on disk");
            }

            var content = await File.ReadAllTextAsync(file.FilePath, cancellationToken);

            // 3. Check if file needs chunking (small files don't benefit from RAG)
            if (!_chunkingService.NeedsChunking(content, 2000))
            {
                _logger.LogInformation("[INGEST] File is small enough, skipping chunking for direct LLM use");
                return new IngestFileResult(true, 0);
            }

            // 4. Initialize vector store
            await _vectorStore.InitializeAsync(cancellationToken);

            // 5. Delete any existing chunks for this file (re-ingest scenario)
            await _vectorStore.DeleteByFileIdAsync(request.FileId, cancellationToken);

            // 6. Chunk the document
            var rawChunks = _chunkingService.ChunkOpenApiDocument(content, request.FileId);
            var chunkList = rawChunks.ToList();
            
            _logger.LogInformation("[INGEST] Created {ChunkCount} chunks", chunkList.Count);

            if (chunkList.Count == 0)
            {
                _logger.LogWarning("[INGEST] No chunks created from document");
                return new IngestFileResult(true, 0);
            }

            // 7. Generate embeddings for each chunk
            var chunkTexts = chunkList.Select(c => c.Content).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunkTexts, cancellationToken);
            var embeddingList = embeddings.ToList();

            _logger.LogInformation("[INGEST] Generated {EmbeddingCount} embeddings", embeddingList.Count);

            // 8. Create document chunks with embeddings
            var documentChunks = new List<DocumentChunk>();
            for (int i = 0; i < chunkList.Count; i++)
            {
                var rawChunk = chunkList[i];
                documentChunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    FileId = request.FileId,
                    Content = rawChunk.Content,
                    Embedding = embeddingList[i],
                    Metadata = rawChunk.Metadata
                });
            }

            // 9. Store in vector database
            await _vectorStore.UpsertBatchAsync(documentChunks, cancellationToken);

            _logger.LogInformation("[INGEST] Successfully ingested {ChunkCount} chunks for file {FileId}",
                documentChunks.Count, request.FileId);

            return new IngestFileResult(true, documentChunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[INGEST] Failed to ingest file {FileId}", request.FileId);
            return new IngestFileResult(false, 0, ex.Message);
        }
    }
}
