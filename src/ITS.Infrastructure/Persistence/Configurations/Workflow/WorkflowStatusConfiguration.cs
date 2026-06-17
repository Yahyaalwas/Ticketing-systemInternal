using ITS.Domain.Entities.Workflow;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowStatusConfiguration : IEntityTypeConfiguration<WorkflowStatus>
{
    public void Configure(EntityTypeBuilder<WorkflowStatus> builder)
    {
        builder.ToTable("WorkflowStatuses", "workflow");
        builder.HasKey(ws => ws.Id);
        builder.Property(ws => ws.Id).HasColumnName("WorkflowStatusId").UseIdentityColumn();

        builder.Property(ws => ws.Name).IsRequired().HasMaxLength(100);
        builder.Property(ws => ws.Description).HasMaxLength(500);
        builder.Property(ws => ws.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(ws => ws.Color).HasMaxLength(20);

        builder.HasIndex(ws => new { ws.WorkflowId, ws.Name })
            .IsUnique()
            .HasDatabaseName("UX_WorkflowStatuses_WorkflowId_Name");

        builder.HasIndex(ws => new { ws.WorkflowId, ws.DisplayOrder })
            .HasDatabaseName("IX_WorkflowStatuses_WorkflowId_DisplayOrder");
    }
}
