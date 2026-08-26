using ArazzoWorkflowPlatform.Domain.Entities;

namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Repository interface for WorkflowEntity operations
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<WorkflowEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<WorkflowEntity>> GetByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken = default);
    Task<WorkflowEntity> CreateAsync(WorkflowEntity workflow, CancellationToken cancellationToken = default);
    Task<WorkflowEntity> UpdateAsync(WorkflowEntity workflow, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
