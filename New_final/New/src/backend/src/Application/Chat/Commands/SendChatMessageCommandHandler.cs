using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;

namespace Application.Chat.Commands;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, SendChatMessageResponse>
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly Kernel? _kernel;
    private readonly ILogger<SendChatMessageCommandHandler> _logger;

    public SendChatMessageCommandHandler(
        IInteractionRepository interactionRepository,
        IWorkflowRepository workflowRepository,
        ILogger<SendChatMessageCommandHandler> logger,
        Kernel? kernel = null)
    {
        _interactionRepository = interactionRepository;
        _workflowRepository = workflowRepository;
        _logger = logger;
        _kernel = kernel;
    }

    public async Task<SendChatMessageResponse> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        var userInteraction = new InteractionHistory
        {
            Id = Guid.NewGuid(),
            WorkflowId = request.WorkflowId,
            SessionId = request.SessionId,
            MessageType = MessageType.UserMessage,
            Role = MessageRole.User,
            Content = request.Message,
            CreatedAt = DateTime.UtcNow
        };
        await _interactionRepository.CreateAsync(userInteraction, cancellationToken);

        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (workflow == null)
        {
            throw new ArgumentException($"Workflow with ID {request.WorkflowId} not found");
        }

        var history = await _interactionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);

        var response = await GenerateResponseAsync(request.Message, workflow, history);

        var assistantInteraction = new InteractionHistory
        {
            Id = Guid.NewGuid(),
            WorkflowId = request.WorkflowId,
            SessionId = request.SessionId,
            MessageType = MessageType.AssistantMessage,
            Role = MessageRole.Assistant,
            Content = response,
            CreatedAt = DateTime.UtcNow
        };
        await _interactionRepository.CreateAsync(assistantInteraction, cancellationToken);

        return new SendChatMessageResponse
        {
            Response = response,
            InteractionId = assistantInteraction.Id
        };
    }

    private async Task<string> GenerateResponseAsync(string message, WorkflowEntity workflow, IEnumerable<InteractionHistory> history)
    {
        if (_kernel == null)
        {
            _logger.LogWarning("[CHAT] Kernel not configured!");
            return "LLM is not configured. Please check the Ollama configuration.";
        }

        var conversationContext = string.Join("\n", history.TakeLast(10).Select(h => $"{h.Role}: {h.Content}"));

        var prompt = $@"You are an AI assistant helping with API workflow analysis and C# code generation.

Workflow Context:
- Workflow Name: {workflow.Name}
- Description: {workflow.Description}
- Arazzo Specification: {workflow.ArazzoJson}

Recent Conversation:
{conversationContext}

User Message: {message}

Instructions:
- If the user asks about the workflow, explain it clearly
- If the user requests code generation, generate clean, production-ready C# code
- If the user asks for changes, modify the workflow or code accordingly
- Be helpful and concise

Your Response:";

        _logger.LogDebug("[CHAT] User message: {Message}", message);
        _logger.LogInformation("[CHAT] Sending prompt to LLM (length: {PromptLength})", prompt.Length);

        try
        {
            var result = await _kernel.InvokePromptAsync(prompt);
            var response = result.ToString();

            _logger.LogInformation("[CHAT] Received response from LLM!");
            _logger.LogDebug("[CHAT] Response length: {ResponseLength} characters", response.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CHAT] Error generating response");
            return $"Error generating response: {ex.Message}";
        }
    }
}