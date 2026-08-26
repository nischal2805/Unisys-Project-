namespace ArazzoWorkflowPlatform.Domain.Entities;

/// <summary>
/// Represents an uploaded OpenAPI specification file
/// </summary>
public class UploadedFile
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string OriginalName { get; set; }
    public required string FilePath { get; set; }
    public long FileSize { get; set; }
    public required string ContentType { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public FileStatus Status { get; set; } = FileStatus.Uploaded;
    public string? Checksum { get; set; }
    public string? Metadata { get; set; } // Additional metadata as JSON

    // Navigation properties
    public ICollection<WorkflowEntity> Workflows { get; set; } = new List<WorkflowEntity>();
}

/// <summary>
/// Status of an uploaded file
/// </summary>
public enum FileStatus
{
    Uploaded,
    Processing,
    Ready,
    Error,
    Deleted
}
