using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Files.Commands;

public record DeleteFileCommand : IRequest<Unit>
{
    public required Guid FileId { get; init; }
}

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Unit>
{
    private readonly IFileRepository _fileRepository;

    public DeleteFileCommandHandler(IFileRepository fileRepository)
    {
        _fileRepository = fileRepository;
    }

    public async Task<Unit> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var exists = await _fileRepository.ExistsAsync(request.FileId, cancellationToken);
        if (!exists)
            throw new ArgumentException($"File with ID {request.FileId} not found");

        await _fileRepository.DeleteAsync(request.FileId, cancellationToken);
        return Unit.Value;
    }
}
