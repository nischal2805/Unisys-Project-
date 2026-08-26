using MediatR;

namespace Application.Workflows.Commands;

public record CreateWorkflowCommand : IRequest<CreateWorkflowResponse>
{
    public required Guid FileId { get; init; }
    public required string WorkflowName { get; init; }
    public string? Description { get; init; }
}

public record CreateWorkflowResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? FileId { get; init; }
    public required string ArazzoJson { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}

