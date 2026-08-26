using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories;
public class FileRepository : IFileRepository
{
    private readonly ApplicationDbContext _context;
    public FileRepository(ApplicationDbContext context) { _context = context; }
    public async Task<UploadedFile?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
    public async Task<List<UploadedFile>> GetAllAsync(CancellationToken ct = default) => await _context.Files.AsNoTracking().OrderByDescending(f => f.UploadedAt).ToListAsync(ct);
    public async Task<UploadedFile> CreateAsync(UploadedFile file, CancellationToken ct = default) { _context.Files.Add(file); await _context.SaveChangesAsync(ct); return file; }
    public async Task<UploadedFile> UpdateAsync(UploadedFile file, CancellationToken ct = default) { _context.Files.Update(file); await _context.SaveChangesAsync(ct); return file; }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default) { var file = await _context.Files.FindAsync(new object[] { id }, ct); if (file != null) { _context.Files.Remove(file); await _context.SaveChangesAsync(ct); } }
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => await _context.Files.AnyAsync(f => f.Id == id, ct);
}
