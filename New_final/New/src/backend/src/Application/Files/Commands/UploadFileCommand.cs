using MediatR;

namespace Application.Files.Commands;

public record UploadFileCommand : IRequest<UploadFileResponse> 
{ 
    public required string FileName { get; init; } 
    public required string ContentType { get; init; } 
    public required long FileSize { get; init; } 
    public required byte[] FileContent { get; init; } 
}

public record UploadFileResponse 
{ 
    public required Guid FileId { get; init; } 
    public required string FileName { get; init; } 
    public required string FilePath { get; init; } 
}
