using MediatR;

namespace Application.Chat.Commands;

public record SendChatMessageCommand : IRequest<SendChatMessageResponse>
{
    public required Guid WorkflowId { get; init; }
    public required Guid SessionId { get; init; }
    public required string Message { get; init; }
}

public record SendChatMessageResponse
{
    public required string Response { get; init; }
    public required Guid InteractionId { get; init; }
}

