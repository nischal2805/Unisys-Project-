using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace Application.Workflows.Commands;

public class RefineWorkflowCommandHandler : IRequestHandler<RefineWorkflowCommand, RefineWorkflowResponse>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly Kernel? _kernel;
    private readonly ILogger<RefineWorkflowCommandHandler> _logger;

    public RefineWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        ILogger<RefineWorkflowCommandHandler> logger,
        Kernel? kernel = null)
    {
        _workflowRepository = workflowRepository;
        _logger = logger;
        _kernel = kernel;
    }

    public async Task<RefineWorkflowResponse> Handle(RefineWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (workflow == null)
            throw new ArgumentException($"Workflow with ID {request.WorkflowId} not found");

        _logger.LogInformation("[REFINE] Refining workflow: {WorkflowName}", workflow.Name);
        _logger.LogDebug("[REFINE] Suggestions: {Suggestions}", request.Suggestions);

        string refinedJson;

        if (_kernel != null && !string.IsNullOrEmpty(request.Suggestions))
        {
            try
            {
                var prompt = $@"You are an expert at refining Arazzo workflow specifications.

Current Arazzo Workflow:
{workflow.ArazzoJson}

User Suggestions:
{request.Suggestions}

Instructions:
1. Apply the user's suggestions to improve the workflow
2. Maintain valid Arazzo 1.0.0 JSON structure
3. Return ONLY the refined JSON, no explanations

Refined Arazzo JSON:";

                var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                var response = result.ToString().Trim();
                
                // Clean up the response
                response = response.Replace("```json", "").Replace("```", "").Trim();

                // Validate JSON
                try
                {
                    JsonDocument.Parse(response);
                    refinedJson = response;
                    _logger.LogInformation("[REFINE] AI refinement successful");
                }
                catch (JsonException)
                {
                    _logger.LogWarning("[REFINE] AI returned invalid JSON, keeping original");
                    refinedJson = workflow.ArazzoJson;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REFINE] AI refinement failed");
                refinedJson = workflow.ArazzoJson;
            }
        }
        else
        {
            refinedJson = workflow.ArazzoJson;
        }

        workflow.ArazzoJson = refinedJson;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.Version++;

        await _workflowRepository.UpdateAsync(workflow, cancellationToken);

        return new RefineWorkflowResponse
        {
            Id = workflow.Id.ToString(),
            Name = workflow.Name,
            ArazzoJson = workflow.ArazzoJson,
            Status = workflow.Status.ToString(),
            UpdatedAt = workflow.UpdatedAt
        };
    }
}
