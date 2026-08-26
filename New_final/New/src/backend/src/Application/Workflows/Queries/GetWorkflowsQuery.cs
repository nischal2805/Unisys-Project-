using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
namespace Application.Workflows.Queries;
public record GetWorkflowsQuery : IRequest<GetWorkflowsResponse>;
public record GetWorkflowsResponse { public required IEnumerable<WorkflowDto> Workflows { get; init; } }
public record WorkflowDto 
{ 
    public required string Id { get; init; } 
    public required string Name { get; init; } 
    public string? Description { get; init; } 
    public string? FileId { get; init; }
    public string? ArazzoJson { get; init; }
    public string Status { get; init; } = "Draft";
    public required DateTime CreatedAt { get; init; } 
    public required DateTime UpdatedAt { get; init; }
}
public class GetWorkflowsQueryHandler : IRequestHandler<GetWorkflowsQuery, GetWorkflowsResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    public GetWorkflowsQueryHandler(IWorkflowRepository workflowRepository) { _workflowRepository = workflowRepository; }
    public async Task<GetWorkflowsResponse> Handle(GetWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var workflows = await _workflowRepository.GetAllAsync(cancellationToken);
        var workflowDtos = workflows.Select(w => new WorkflowDto 
        { 
            Id = w.Id.ToString(), 
            Name = w.Name, 
            Description = w.Description, 
            FileId = w.FileId?.ToString(),
            ArazzoJson = w.ArazzoJson, // Keep as string for frontend to parse
            Status = w.Status.ToString(),
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        });
        return new GetWorkflowsResponse { Workflows = workflowDtos };
    }
}
