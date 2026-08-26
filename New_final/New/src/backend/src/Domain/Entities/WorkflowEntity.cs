namespace ArazzoWorkflowPlatform.Domain.Entities;

/// <summary>
/// Represents a workflow entity in the system
/// </summary>
public class WorkflowEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? FileId { get; set; }
    public required string ArazzoJson { get; set; } // Stored as JSON string
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public int Version { get; set; } = 1;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; } // Additional metadata as JSON

    // Navigation properties
    public UploadedFile? File { get; set; }
    public ICollection<InteractionHistory> Interactions { get; set; } = new List<InteractionHistory>();
    public ICollection<WorkflowStepEntity> Steps { get; set; } = new List<WorkflowStepEntity>();
}

/// <summary>
/// Status of a workflow
/// </summary>
public enum WorkflowStatus
{
    Draft,
    InProgress,
    Completed,
    Failed,
    Archived
}
