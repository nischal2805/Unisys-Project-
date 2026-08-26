namespace ArazzoWorkflowPlatform.Domain.Entities;

/// <summary>
/// Represents a chat interaction or system event in the workflow development process
/// </summary>
public class InteractionHistory
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid SessionId { get; set; }
    public MessageType MessageType { get; set; }
    public MessageRole Role { get; set; }
    public required string Content { get; set; }
    public int? TokensUsed { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public string? ContextUsed { get; set; } // JSON array of context sources
    public string? Metadata { get; set; } // Additional metadata as JSON
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public WorkflowEntity? Workflow { get; set; }
}

/// <summary>
/// Type of message in the interaction
/// </summary>
public enum MessageType
{
    UserMessage,
    AssistantMessage,
    SystemMessage,
    ErrorMessage
}

/// <summary>
/// Role of the message sender
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System
}
