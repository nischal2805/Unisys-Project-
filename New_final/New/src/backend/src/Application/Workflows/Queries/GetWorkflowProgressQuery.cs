using MediatR;

namespace Application.Workflows.Queries;

public record GetWorkflowProgressQuery : IRequest<GetWorkflowProgressResponse>
{
    public required Guid WorkflowId { get; init; }
}

public record GetWorkflowProgressResponse
{
    public required string WorkflowId { get; init; }
    public required string Status { get; init; }
    public int Progress { get; init; }
    public string? CurrentStep { get; init; }
    public int TotalSteps { get; init; }
    public int CompletedSteps { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}
