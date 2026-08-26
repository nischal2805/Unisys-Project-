using ArazzoWorkflowPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<UploadedFile> Files { get; set; }
    public DbSet<WorkflowEntity> Workflows { get; set; }
    public DbSet<WorkflowStepEntity> WorkflowSteps { get; set; }
    public DbSet<InteractionHistory> Interactions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UploadedFile>(e => { e.ToTable("uploaded_files"); e.HasMany(f => f.Workflows).WithOne(w => w.File).HasForeignKey(w => w.FileId).OnDelete(DeleteBehavior.SetNull); });
        modelBuilder.Entity<WorkflowEntity>(e => { e.ToTable("workflows"); e.HasMany(w => w.Steps).WithOne(s => s.Workflow).HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade); e.HasMany(w => w.Interactions).WithOne(i => i.Workflow).HasForeignKey(i => i.WorkflowId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<WorkflowStepEntity>(e => { e.ToTable("workflow_steps"); e.HasIndex(x => x.WorkflowId); });
        modelBuilder.Entity<InteractionHistory>(e => { e.ToTable("interaction_history"); e.HasIndex(x => x.WorkflowId); e.HasIndex(x => x.SessionId); });
    }
}
