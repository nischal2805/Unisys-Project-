using MediatR;

namespace Application.Workflows.Commands;

public record UpdateWorkflowCommand : IRequest<UpdateWorkflowResponse>
{
    public required Guid WorkflowId { get; init; }
    public required string ArazzoJson { get; init; }
}

public record UpdateWorkflowResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ArazzoJson { get; init; }
    public required string Status { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
