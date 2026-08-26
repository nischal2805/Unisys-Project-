using ArazzoWorkflowPlatform.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Infrastructure.VectorStore;

/// <summary>
/// Qdrant implementation of vector store for storing and querying OpenAPI embeddings
/// </summary>
public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly string _collectionName;
    private readonly int _vectorSize;
    private bool _initialized;

    public QdrantVectorStore(
        IConfiguration configuration,
        ILogger<QdrantVectorStore> logger)
    {
        _logger = logger;
        
        // Support both Qdrant:Endpoint (URL format) and Qdrant:Host/Port format
        var endpoint = configuration["Qdrant:Endpoint"];
        string host;
        int port;
        
        if (!string.IsNullOrEmpty(endpoint))
        {
            // Parse endpoint URL (e.g., http://qdrant:6333)
            var uri = new Uri(endpoint);
            host = uri.Host;
            // Use gRPC port (6334) for the client, not HTTP port (6333)
            port = uri.Port == 6333 ? 6334 : uri.Port;
        }
        else
        {
            host = configuration["Qdrant:Host"] ?? "localhost";
            port = int.Parse(configuration["Qdrant:Port"] ?? "6334");
        }
        
        _collectionName = configuration["Qdrant:CollectionName"] ?? "openapi_embeddings";
        _vectorSize = int.Parse(configuration["Qdrant:VectorSize"] ?? "768"); // nomic-embed-text default
        
        _logger.LogInformation("[QDRANT] Connecting to Qdrant at {Host}:{Port}", host, port);
        _client = new QdrantClient(host, port);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            var exists = collections.Any(c => c == _collectionName);

            if (!exists)
            {
                _logger.LogInformation("[QDRANT] Creating collection {CollectionName} with vector size {VectorSize}", 
                    _collectionName, _vectorSize);
                
                await _client.CreateCollectionAsync(
                    _collectionName,
                    new VectorParams
                    {
                        Size = (ulong)_vectorSize,
                        Distance = Distance.Cosine
                    },
                    cancellationToken: cancellationToken);
                
                // Create payload indexes for efficient filtering
                await _client.CreatePayloadIndexAsync(
                    _collectionName,
                    "file_id",
                    PayloadSchemaType.Keyword,
                    cancellationToken: cancellationToken);
                
                _logger.LogInformation("[QDRANT] Collection {CollectionName} created successfully", _collectionName);
            }
            else
            {
                _logger.LogDebug("[QDRANT] Collection {CollectionName} already exists", _collectionName);
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to initialize collection");
            throw;
        }
    }

    public async Task UpsertAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        
        try
        {
            var point = CreatePointFromChunk(chunk);
            await _client.UpsertAsync(_collectionName, new[] { point }, cancellationToken: cancellationToken);
            
            _logger.LogDebug("[QDRANT] Upserted chunk {ChunkId} for file {FileId}", chunk.Id, chunk.FileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to upsert chunk {ChunkId}", chunk.Id);
            throw;
        }
    }

    public async Task UpsertBatchAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        
        var chunkList = chunks.ToList();
        if (chunkList.Count == 0) return;

        try
        {
            var points = chunkList.Select(CreatePointFromChunk).ToList();
            
            // Batch in groups of 100 for efficiency
            const int batchSize = 100;
            for (int i = 0; i < points.Count; i += batchSize)
            {
                var batch = points.Skip(i).Take(batchSize).ToList();
                await _client.UpsertAsync(_collectionName, batch, cancellationToken: cancellationToken);
            }
            
            _logger.LogInformation("[QDRANT] Upserted {Count} chunks in batches", chunkList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to upsert batch of {Count} chunks", chunkList.Count);
            throw;
        }
    }

    public async Task<IEnumerable<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int limit = 5,
        Guid? filterByFileId = null,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        
        try
        {
            Filter? filter = null;
            if (filterByFileId.HasValue)
            {
                filter = new Filter
                {
                    Must = { new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "file_id",
                            Match = new Match { Keyword = filterByFileId.Value.ToString() }
                        }
                    }}
                };
            }

            var results = await _client.SearchAsync(
                _collectionName,
                queryEmbedding,
                limit: (ulong)limit,
                filter: filter,
                payloadSelector: true,
                cancellationToken: cancellationToken);

            return results.Select(r => new VectorSearchResult
            {
                ChunkId = Guid.Parse(r.Id.Uuid),
                FileId = Guid.Parse(r.Payload["file_id"].StringValue),
                Content = r.Payload["content"].StringValue,
                Metadata = new ChunkMetadata
                {
                    ChunkType = r.Payload["chunk_type"].StringValue,
                    Path = r.Payload.TryGetValue("path", out var path) ? path.StringValue : null,
                    Method = r.Payload.TryGetValue("method", out var method) ? method.StringValue : null,
                    OperationId = r.Payload.TryGetValue("operation_id", out var opId) ? opId.StringValue : null,
                    SchemaName = r.Payload.TryGetValue("schema_name", out var schema) ? schema.StringValue : null,
                    ChunkIndex = (int)(r.Payload.TryGetValue("chunk_index", out var idx) ? idx.IntegerValue : 0),
                    TotalChunks = (int)(r.Payload.TryGetValue("total_chunks", out var total) ? total.IntegerValue : 0)
                },
                Score = r.Score
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Search failed");
            throw;
        }
    }

    public async Task DeleteByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        
        try
        {
            var filter = new Filter
            {
                Must = { new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "file_id",
                        Match = new Match { Keyword = fileId.ToString() }
                    }
                }}
            };

            await _client.DeleteAsync(_collectionName, filter, cancellationToken: cancellationToken);
            _logger.LogInformation("[QDRANT] Deleted chunks for file {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to delete chunks for file {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> HasChunksForFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var count = await GetChunkCountAsync(fileId, cancellationToken);
        return count > 0;
    }

    public async Task<int> GetChunkCountAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        
        try
        {
            var filter = new Filter
            {
                Must = { new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "file_id",
                        Match = new Match { Keyword = fileId.ToString() }
                    }
                }}
            };

            var result = await _client.CountAsync(_collectionName, filter, cancellationToken: cancellationToken);
            return (int)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to count chunks for file {FileId}", fileId);
            return 0;
        }
    }

    private PointStruct CreatePointFromChunk(DocumentChunk chunk)
    {
        var payload = new Dictionary<string, Value>
        {
            ["file_id"] = new Value { StringValue = chunk.FileId.ToString() },
            ["content"] = new Value { StringValue = chunk.Content },
            ["chunk_type"] = new Value { StringValue = chunk.Metadata.ChunkType },
            ["chunk_index"] = new Value { IntegerValue = chunk.Metadata.ChunkIndex },
            ["total_chunks"] = new Value { IntegerValue = chunk.Metadata.TotalChunks }
        };

        if (!string.IsNullOrEmpty(chunk.Metadata.Path))
            payload["path"] = new Value { StringValue = chunk.Metadata.Path };
        if (!string.IsNullOrEmpty(chunk.Metadata.Method))
            payload["method"] = new Value { StringValue = chunk.Metadata.Method };
        if (!string.IsNullOrEmpty(chunk.Metadata.OperationId))
            payload["operation_id"] = new Value { StringValue = chunk.Metadata.OperationId };
        if (!string.IsNullOrEmpty(chunk.Metadata.SchemaName))
            payload["schema_name"] = new Value { StringValue = chunk.Metadata.SchemaName };

        return new PointStruct
        {
            Id = new PointId { Uuid = chunk.Id.ToString() },
            Vectors = chunk.Embedding,
            Payload = { payload }
        };
    }
}
