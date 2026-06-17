using ITS.Domain.Entities.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Workflow;

public class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions", "workflow");
        builder.HasKey(wt => wt.Id);
        builder.Property(wt => wt.Id).HasColumnName("WorkflowTransitionId").UseIdentityColumn();

        builder.Property(wt => wt.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(wt => new { wt.WorkflowId, wt.FromStatusId, wt.ToStatusId })
            .IsUnique()
            .HasDatabaseName("UX_WorkflowTransitions_WorkflowId_From_To");

        // NoAction avoids multiple cascade paths from WorkflowStatuses → WorkflowTransitions (FromStatus and ToStatus)
        builder.HasOne(wt => wt.FromStatus)
            .WithMany()
            .HasForeignKey(wt => wt.FromStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(wt => wt.ToStatus)
            .WithMany()
            .HasForeignKey(wt => wt.ToStatusId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(wt => wt.Guards)
            .WithOne()
            .HasForeignKey(g => g.TransitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(wt => wt.Actions)
            .WithOne()
            .HasForeignKey(a => a.TransitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
