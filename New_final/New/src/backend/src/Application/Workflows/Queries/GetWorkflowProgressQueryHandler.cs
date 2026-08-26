using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace Application.Workflows.Queries;

public class GetWorkflowProgressQueryHandler : IRequestHandler<GetWorkflowProgressQuery, GetWorkflowProgressResponse>
{
    private readonly IWorkflowRepository _workflowRepository;

    public GetWorkflowProgressQueryHandler(IWorkflowRepository workflowRepository)
    {
        _workflowRepository = workflowRepository;
    }

    public async Task<GetWorkflowProgressResponse> Handle(GetWorkflowProgressQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null)
            throw new ArgumentException($"Workflow with ID {request.WorkflowId} not found");

        // Calculate progress based on workflow status and steps
        int totalSteps = 0;
        int completedSteps = 0;
        string? currentStep = null;

        try
        {
            var arazzoDoc = JsonDocument.Parse(workflow.ArazzoJson);
            if (arazzoDoc.RootElement.TryGetProperty("workflows", out var workflows) && 
                workflows.GetArrayLength() > 0)
            {
                var firstWorkflow = workflows[0];
                if (firstWorkflow.TryGetProperty("steps", out var steps))
                {
                    totalSteps = steps.GetArrayLength();
                    
                    // Simulate progress based on status
                    switch (workflow.Status)
                    {
                        case WorkflowStatus.Draft:
                            completedSteps = 0;
                            currentStep = "Workflow draft created";
                            break;
                        case WorkflowStatus.InProgress:
                            completedSteps = totalSteps / 2;
                            currentStep = "Processing workflow steps";
                            break;
                        case WorkflowStatus.Completed:
                            completedSteps = totalSteps;
                            currentStep = "All steps completed";
                            break;
                        case WorkflowStatus.Failed:
                            currentStep = "Workflow failed";
                            break;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // If JSON parsing fails, use default values
        }

        int progress = totalSteps > 0 ? (completedSteps * 100) / totalSteps : 0;
        if (workflow.Status == WorkflowStatus.Completed) progress = 100;
        if (workflow.Status == WorkflowStatus.Draft) progress = 100; // Draft is complete in its creation

        return new GetWorkflowProgressResponse
        {
            WorkflowId = workflow.Id.ToString(),
            Status = workflow.Status.ToString(),
            Progress = progress,
            CurrentStep = currentStep,
            TotalSteps = totalSteps,
            CompletedSteps = completedSteps,
            StartedAt = workflow.CreatedAt,
            CompletedAt = workflow.CompletedAt,
            ErrorMessage = workflow.ErrorMessage
        };
    }
}
