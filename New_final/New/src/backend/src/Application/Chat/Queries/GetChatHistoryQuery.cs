using ArazzoWorkflowPlatform.Domain.Interfaces;
using MediatR;

namespace Application.Chat.Queries;

public record GetChatHistoryQuery : IRequest<GetChatHistoryResponse>
{
    public required Guid SessionId { get; init; }
}

public record GetChatHistoryResponse
{
    public required IEnumerable<ChatMessageDto> Messages { get; init; }
}

public record ChatMessageDto
{
    public required string Id { get; init; }
    public required string Role { get; init; }
    public required string Content { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, GetChatHistoryResponse>
{
    private readonly IInteractionRepository _interactionRepository;

    public GetChatHistoryQueryHandler(IInteractionRepository interactionRepository)
    {
        _interactionRepository = interactionRepository;
    }

    public async Task<GetChatHistoryResponse> Handle(GetChatHistoryQuery request, CancellationToken cancellationToken)
    {
        var interactions = await _interactionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);

        var messages = interactions
            .OrderBy(i => i.CreatedAt)
            .Select(i => new ChatMessageDto
            {
                Id = i.Id.ToString(),
                Role = i.Role.ToString().ToLower(),
                Content = i.Content,
                Timestamp = i.CreatedAt
            });

        return new GetChatHistoryResponse { Messages = messages };
    }
}
