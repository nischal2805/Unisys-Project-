using Application.Workflows.Commands;
using Application.Workflows.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(IMediator mediator, ILogger<WorkflowsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<CreateWorkflowResponse>> CreateWorkflow(
        [FromBody] CreateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for workflow creation: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating workflow: {WorkflowName} for file: {FileId}", 
                request.WorkflowName, request.FileId);

            var command = new CreateWorkflowCommand
            {
                FileId = request.FileId,
                WorkflowName = request.WorkflowName,
                Description = request.Description
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error creating workflow");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<GetWorkflowsResponse>> GetWorkflows(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetWorkflowsQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetWorkflowByIdResponse>> GetWorkflowById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetWorkflowByIdQuery { WorkflowId = id };
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
                return NotFound(new { error = $"Workflow with ID {id} not found" });
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UpdateWorkflowResponse>> UpdateWorkflow(
        Guid id,
        [FromBody] UpdateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating workflow: {Id}", id);

            var command = new UpdateWorkflowCommand
            {
                WorkflowId = id,
                ArazzoJson = request.ArazzoWorkflow
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error updating workflow");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating workflow");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost("{id}/refine")]
    public async Task<ActionResult<RefineWorkflowResponse>> RefineWorkflow(
        Guid id,
        [FromBody] RefineWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refining workflow: {Id} with suggestions", id);

            var command = new RefineWorkflowCommand
            {
                WorkflowId = id,
                Suggestions = request.Suggestions
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining workflow");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("{id}/generate")]
    public async Task<IActionResult> GenerateService(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating service code for workflow: {Id}", id);

            var command = new GenerateServiceCommand { WorkflowId = id };
            var result = await _mediator.Send(command, cancellationToken);

            return File(result.ZipContent, "application/zip", result.FileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Workflow not found for service generation");
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating service");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("{id}/progress")]
    public async Task<ActionResult<GetWorkflowProgressResponse>> GetWorkflowProgress(
        Guid id, 
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetWorkflowProgressQuery { WorkflowId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow progress");
            return StatusCode(500, "Internal server error");
        }
    }
}

public record CreateWorkflowRequest
{
    public required Guid FileId { get; init; }
    public required string WorkflowName { get; init; }
    public string? Description { get; init; }
}

public record UpdateWorkflowRequest
{
    public required string ArazzoWorkflow { get; init; }
}

public record RefineWorkflowRequest
{
    public required string Suggestions { get; init; }
}
