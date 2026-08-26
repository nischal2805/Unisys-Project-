using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Files.Queries;

public record GetFileByIdQuery : IRequest<GetFileByIdResponse?>
{
    public required Guid FileId { get; init; }
}

public record GetFileByIdResponse
{
    public required string Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required DateTime UploadedAt { get; init; }
    public string? Metadata { get; init; }
}

public class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, GetFileByIdResponse?>
{
    private readonly IFileRepository _fileRepository;

    public GetFileByIdQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<GetFileByIdResponse?> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        
        if (file == null)
            return null;

        return new GetFileByIdResponse
        {
            Id = file.Id.ToString(),
            FileName = file.Name,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            UploadedAt = file.UploadedAt,
            Metadata = file.Metadata
        };
    }
}
