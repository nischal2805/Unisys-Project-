using ArazzoWorkflowPlatform.Domain.Entities;

namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Repository interface for InteractionHistory operations
/// </summary>
public interface IInteractionRepository
{
    Task<InteractionHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<InteractionHistory>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<List<InteractionHistory>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<InteractionHistory> CreateAsync(InteractionHistory interaction, CancellationToken cancellationToken = default);
    Task<List<InteractionHistory>> CreateBatchAsync(List<InteractionHistory> interactions, CancellationToken cancellationToken = default);
}
