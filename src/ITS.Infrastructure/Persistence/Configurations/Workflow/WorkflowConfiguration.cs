using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEntity = ITS.Domain.Entities.Workflow.Workflow;

namespace ITS.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowConfiguration : IEntityTypeConfiguration<WorkflowEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowEntity> builder)
    {
        builder.ToTable("Workflows", "workflow");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("WorkflowId").HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(w => w.Name).IsRequired().HasMaxLength(256);
        builder.Property(w => w.Description).HasMaxLength(1000);

        builder.Property(w => w.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(w => w.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(w => w.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(w => w.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(w => new { w.IsTemplate, w.Name })
            .HasDatabaseName("IX_Workflows_IsTemplate_Name");

        builder.HasIndex(w => w.ProjectId)
            .HasFilter("[ProjectId] IS NOT NULL")
            .HasDatabaseName("IX_Workflows_ProjectId");

        builder.HasMany(w => w.Statuses)
            .WithOne()
            .HasForeignKey(ws => ws.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Transitions)
            .WithOne()
            .HasForeignKey(wt => wt.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
