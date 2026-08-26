using ArazzoWorkflowPlatform.Domain.Entities;

namespace ArazzoWorkflowPlatform.Domain.Interfaces;

/// <summary>
/// Repository interface for UploadedFile operations
/// </summary>
public interface IFileRepository
{
    Task<UploadedFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<UploadedFile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UploadedFile> CreateAsync(UploadedFile file, CancellationToken cancellationToken = default);
    Task<UploadedFile> UpdateAsync(UploadedFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
