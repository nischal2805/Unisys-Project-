using ArazzoWorkflowPlatform.Application.Files.Commands;
using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Application.Files.Commands;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResponse>
{
    private readonly IFileRepository _fileRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<UploadFileCommandHandler> _logger;
    private readonly IChunkingService? _chunkingService;

    public UploadFileCommandHandler(
        IFileRepository fileRepository,
        IMediator mediator,
        ILogger<UploadFileCommandHandler> logger,
        IChunkingService? chunkingService = null)
    {
        _fileRepository = fileRepository;
        _mediator = mediator;
        _logger = logger;
        _chunkingService = chunkingService;
    }

    public async Task<UploadFileResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[UPLOAD] Processing file: {FileName}", request.FileName);
            _logger.LogDebug("[UPLOAD] Content Type: {ContentType}", request.ContentType);
            _logger.LogDebug("[UPLOAD] File Size: {FileSize} bytes", request.FileSize);
            _logger.LogDebug("[UPLOAD] Content Length: {ContentLength} bytes", request.FileContent?.Length ?? 0);
            
            // Parse content if it's a text-based file
            var shouldParseAsText = request.ContentType.Contains("json") || 
                                   request.ContentType.Contains("yaml") || 
                                   request.ContentType.Contains("text") ||
                                   request.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                   request.FileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                                   request.FileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
            
            _logger.LogDebug("[UPLOAD] Should parse as text: {ShouldParse}", shouldParseAsText);
            
            var parsedContent = shouldParseAsText && request.FileContent != null
                ? Encoding.UTF8.GetString(request.FileContent) 
                : null;
                
            _logger.LogDebug("[UPLOAD] Parsed content length: {ContentLength} chars", parsedContent?.Length ?? 0);

            // Generate unique name and path
            var uniqueName = $"{Guid.NewGuid()}_{request.FileName}";
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads", DateTime.UtcNow.ToString("yyyy/MM/dd"));
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, uniqueName);

            // Save file to disk for later processing
            if (request.FileContent != null)
            {
                await File.WriteAllBytesAsync(filePath, request.FileContent, cancellationToken);
                _logger.LogDebug("[UPLOAD] File saved to disk: {FilePath}", filePath);
            }

            // Create file entity
            var file = new UploadedFile
            {
                Id = Guid.NewGuid(),
                Name = uniqueName,
                OriginalName = request.FileName,
                FilePath = filePath,
                FileSize = request.FileSize,
                ContentType = request.ContentType,
                UploadedAt = DateTime.UtcNow,
                Status = FileStatus.Processing,
                Metadata = parsedContent
            };

            // Save to repository
            await _fileRepository.CreateAsync(file, cancellationToken);
            _logger.LogInformation("[UPLOAD] File record created: {FileId}", file.Id);

            // Check if file needs RAG ingestion (large files)
            var needsIngestion = _chunkingService?.NeedsChunking(parsedContent ?? "", 2000) ?? false;
            
            if (needsIngestion)
            {
                _logger.LogInformation("[UPLOAD] Large file detected, triggering RAG ingestion for {FileId}", file.Id);
                
                // Trigger async ingestion (fire and forget for now, can be made more robust with background jobs)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var ingestResult = await _mediator.Send(new IngestFileCommand(file.Id), CancellationToken.None);
                        if (ingestResult.Success)
                        {
                            _logger.LogInformation("[UPLOAD] Ingestion completed: {ChunkCount} chunks created", ingestResult.ChunksCreated);
                        }
                        else
                        {
                            _logger.LogWarning("[UPLOAD] Ingestion failed: {Error}", ingestResult.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[UPLOAD] Ingestion task failed for {FileId}", file.Id);
                    }
                });
            }

            // Update status to Ready
            file.Status = FileStatus.Ready;
            await _fileRepository.UpdateAsync(file, cancellationToken);

            return new UploadFileResponse
            {
                FileId = file.Id,
                FileName = file.OriginalName,
                FilePath = file.FilePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UPLOAD] Failed to upload file: {FileName}", request.FileName);
            throw new InvalidOperationException($"Failed to upload file: {ex.Message}", ex);
        }
    }
}
