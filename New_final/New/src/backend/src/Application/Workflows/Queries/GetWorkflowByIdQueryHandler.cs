using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Workflows.Queries;

public class GetWorkflowByIdQueryHandler : IRequestHandler<GetWorkflowByIdQuery, GetWorkflowByIdResponse?>
{
    private readonly IWorkflowRepository _workflowRepository;

    public GetWorkflowByIdQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<GetWorkflowByIdResponse?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null)
            return null;

        return new GetWorkflowByIdResponse
        {
            Id = workflow.Id.ToString(),
            Name = workflow.Name,
            Description = workflow.Description,
            FileId = workflow.FileId?.ToString(),
            ArazzoJson = workflow.ArazzoJson,
            Status = workflow.Status.ToString(),
            Version = workflow.Version,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt
        };
    }
}
