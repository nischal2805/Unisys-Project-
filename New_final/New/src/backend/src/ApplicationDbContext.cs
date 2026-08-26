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

        modelBuilder.Entity<UploadedFile>(entity =>
        {
            entity.ToTable("uploaded_files");
            entity.HasMany(f => f.Workflows).WithOne(w => w.File).HasForeignKey(w => w.FileId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkflowEntity>(entity =>
        {
            entity.ToTable("workflows");
            entity.HasMany(w => w.Steps).WithOne(s => s.Workflow).HasForeignKey(s => s.WorkflowId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(w => w.Interactions).WithOne(i => i.Workflow).HasForeignKey(i => i.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowStepEntity>(entity =>
        {
            entity.ToTable("workflow_steps");
            entity.HasIndex(e => e.WorkflowId);
        });

        modelBuilder.Entity<InteractionHistory>(entity =>
        {
            entity.ToTable("interaction_history");
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.SessionId);
        });
    }
}
