using MediatR;

namespace Application.Workflows.Commands;

public record RefineWorkflowCommand : IRequest<RefineWorkflowResponse>
{
    public required Guid WorkflowId { get; init; }
    public required string Suggestions { get; init; }
}

public record RefineWorkflowResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ArazzoJson { get; init; }
    public required string Status { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
