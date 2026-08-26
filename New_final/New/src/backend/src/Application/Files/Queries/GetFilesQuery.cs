using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Files.Queries;

public record GetFilesQuery : IRequest<GetFilesResponse>;

public record GetFilesResponse
{
    public required IEnumerable<FileDto> Files { get; init; }
}

public record FileDto
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required DateTime UploadedAt { get; init; }
}

public class GetFilesQueryHandler : IRequestHandler<GetFilesQuery, GetFilesResponse>
{
    private readonly IFileRepository _fileRepository;

    public GetFilesQueryHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<GetFilesResponse> Handle(GetFilesQuery request, CancellationToken cancellationToken)
    {
        var files = await _fileRepository.GetAllAsync(cancellationToken);

        var fileDtos = files.Select(f => new FileDto
        {
            Id = f.Id,
            FileName = f.OriginalName,
            ContentType = f.ContentType,
            FileSize = f.FileSize,
            UploadedAt = f.UploadedAt
        });

        return new GetFilesResponse { Files = fileDtos };
    }
}
