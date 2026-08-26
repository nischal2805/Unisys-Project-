using Application.Chat.Commands;
using Application.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<ActionResult<SendChatMessageResponse>> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Chat message for workflow: {WorkflowId}", request.WorkflowId);

            var command = new SendChatMessageCommand
            {
                WorkflowId = request.WorkflowId,
                SessionId = request.SessionId,
                Message = request.Message
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("history/{sessionId}")]
    public async Task<ActionResult<GetChatHistoryResponse>> GetChatHistory(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting chat history for session: {SessionId}", sessionId);

            var query = new GetChatHistoryQuery { SessionId = sessionId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat history");
            return StatusCode(500, "Internal server error");
        }
    }
}

public record SendMessageRequest
{
    public required Guid WorkflowId { get; init; }
    public required Guid SessionId { get; init; }
    public required string Message { get; init; }
}
