using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ArazzoWorkflowPlatform.Application.Files.Queries;

/// <summary>
/// Handler for retrieving relevant context from the vector store using semantic search
/// </summary>
public class GetRelevantContextQueryHandler : IRequestHandler<GetRelevantContextQuery, RelevantContextResult>
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<GetRelevantContextQueryHandler> _logger;

    public GetRelevantContextQueryHandler(
        IVectorStore vectorStore,
        IEmbeddingService embeddingService,
        ILogger<GetRelevantContextQueryHandler> logger)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<RelevantContextResult> Handle(GetRelevantContextQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[RAG] Retrieving context for query: {Query}", 
                request.Query.Length > 100 ? request.Query[..100] + "..." : request.Query);

            // 1. Generate embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);

            _logger.LogDebug("[RAG] Query embedding generated ({Dimension} dimensions)", queryEmbedding.Length);

            // 2. Search the vector store
            var searchResults = await _vectorStore.SearchAsync(
                queryEmbedding,
                request.MaxResults,
                request.FileId,
                cancellationToken);

            var resultList = searchResults.ToList();

            _logger.LogInformation("[RAG] Found {ResultCount} relevant chunks", resultList.Count);

            // 3. Convert to context chunks
            var chunks = resultList.Select(r => new ContextChunk(
                Content: r.Content,
                ChunkType: r.Metadata.ChunkType,
                Path: r.Metadata.Path,
                Method: r.Metadata.Method,
                OperationId: r.Metadata.OperationId,
                Score: r.Score
            )).ToList();

            return new RelevantContextResult(true, chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RAG] Failed to retrieve context for query");
            return new RelevantContextResult(false, new List<ContextChunk>(), ex.Message);
        }
    }
}
