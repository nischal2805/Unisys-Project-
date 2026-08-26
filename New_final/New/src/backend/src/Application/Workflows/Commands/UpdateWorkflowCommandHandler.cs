using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Workflows.Commands;

public class UpdateWorkflowCommandHandler : IRequestHandler<UpdateWorkflowCommand, UpdateWorkflowResponse>
{
    private readonly IWorkflowRepository _workflowRepository;

    public UpdateWorkflowCommandHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<UpdateWorkflowResponse> Handle(UpdateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (workflow == null)
            throw new ArgumentException($"Workflow with ID {request.WorkflowId} not found");

        workflow.ArazzoJson = request.ArazzoJson;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.Version++;

        await _workflowRepository.UpdateAsync(workflow, cancellationToken);

        return new UpdateWorkflowResponse
        {
            Id = workflow.Id.ToString(),
            Name = workflow.Name,
            ArazzoJson = workflow.ArazzoJson,
            Status = workflow.Status.ToString(),
            UpdatedAt = workflow.UpdatedAt
        };
    }
}
