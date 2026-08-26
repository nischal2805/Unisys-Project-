using MediatR;

namespace Application.Workflows.Commands;

public record GenerateServiceCommand : IRequest<GenerateServiceResponse>
{
    public required Guid WorkflowId { get; init; }
}

public record GenerateServiceResponse
{
    public required byte[] ZipContent { get; init; }
    public required string FileName { get; init; }
}
