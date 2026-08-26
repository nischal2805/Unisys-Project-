namespace ArazzoWorkflowPlatform.Domain.Entities;

/// <summary>
/// Represents a single step within a workflow (denormalized for performance)
/// </summary>
public class WorkflowStepEntity
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public required string StepId { get; set; }
    public int StepOrder { get; set; }
    public required string OperationId { get; set; }
    public string? OperationPath { get; set; }
    public string? Description { get; set; }
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public required string StepJson { get; set; } // Complete step data as JSON
    public int ExecutionCount { get; set; } = 0;
    public DateTime? LastExecutedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public WorkflowEntity? Workflow { get; set; }
}

/// <summary>
/// Status of a workflow step
/// </summary>
public enum StepStatus
{
    Pending,
    Completed,
    Failed,
    Skipped
}
