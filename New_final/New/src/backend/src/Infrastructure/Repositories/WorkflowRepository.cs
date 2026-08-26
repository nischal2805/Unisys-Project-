using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories;
public class WorkflowRepository : IWorkflowRepository
{
    private readonly ApplicationDbContext _context;
    public WorkflowRepository(ApplicationDbContext context) { _context = context; }
    public async Task<WorkflowEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Workflows.Include(w => w.Steps).Include(w => w.File).AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
    public async Task<List<WorkflowEntity>> GetAllAsync(CancellationToken ct = default) => await _context.Workflows.Include(w => w.File).AsNoTracking().OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
    public async Task<List<WorkflowEntity>> GetByStatusAsync(WorkflowStatus status, CancellationToken ct = default) => await _context.Workflows.Include(w => w.File).AsNoTracking().Where(w => w.Status == status).OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
    public async Task<WorkflowEntity> CreateAsync(WorkflowEntity workflow, CancellationToken ct = default) { _context.Workflows.Add(workflow); await _context.SaveChangesAsync(ct); return workflow; }
    public async Task<WorkflowEntity> UpdateAsync(WorkflowEntity workflow, CancellationToken ct = default) { workflow.UpdatedAt = DateTime.UtcNow; _context.Workflows.Update(workflow); await _context.SaveChangesAsync(ct); return workflow; }
    public async Task DeleteAsync(Guid id, CancellationToken ct = default) { var w = await _context.Workflows.FindAsync(new object[] { id }, ct); if (w != null) { _context.Workflows.Remove(w); await _context.SaveChangesAsync(ct); } }
    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => await _context.Workflows.AnyAsync(w => w.Id == id, ct);
}
