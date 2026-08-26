using MediatR;

namespace Application.Workflows.Queries;

public record GetWorkflowByIdQuery : IRequest<GetWorkflowByIdResponse?>
{
    public required Guid WorkflowId { get; init; }
}

public record GetWorkflowByIdResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? FileId { get; init; }
    public required string ArazzoJson { get; init; }
    public required string Status { get; init; }
    public int Version { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
