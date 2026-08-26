using ArazzoWorkflowPlatform.Domain.Entities;
using ArazzoWorkflowPlatform.Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Repositories;
public class InteractionRepository : IInteractionRepository
{
    private readonly ApplicationDbContext _context;
    public InteractionRepository(ApplicationDbContext context) { _context = context; }
    public async Task<InteractionHistory?> GetByIdAsync(Guid id, CancellationToken ct = default) => await _context.Interactions.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
    public async Task<List<InteractionHistory>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken ct = default) => await _context.Interactions.AsNoTracking().Where(i => i.WorkflowId == workflowId).OrderBy(i => i.CreatedAt).ToListAsync(ct);
    public async Task<List<InteractionHistory>> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default) => await _context.Interactions.AsNoTracking().Where(i => i.SessionId == sessionId).OrderBy(i => i.CreatedAt).ToListAsync(ct);
    public async Task<InteractionHistory> CreateAsync(InteractionHistory interaction, CancellationToken ct = default) { _context.Interactions.Add(interaction); await _context.SaveChangesAsync(ct); return interaction; }
    public async Task<List<InteractionHistory>> CreateBatchAsync(List<InteractionHistory> interactions, CancellationToken ct = default) { _context.Interactions.AddRange(interactions); await _context.SaveChangesAsync(ct); return interactions; }
}
