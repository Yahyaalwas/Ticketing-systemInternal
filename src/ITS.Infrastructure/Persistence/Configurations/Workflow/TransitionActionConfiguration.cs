using ITS.Domain.Entities.Workflow;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Workflow;

public class TransitionActionConfiguration : IEntityTypeConfiguration<TransitionAction>
{
    public void Configure(EntityTypeBuilder<TransitionAction> builder)
    {
        builder.ToTable("TransitionActions", "workflow");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("TransitionActionId").UseIdentityColumn();

        builder.Property(a => a.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(a => a.Configuration).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasIndex(a => new { a.TransitionId, a.DisplayOrder })
            .HasDatabaseName("IX_TransitionActions_TransitionId_DisplayOrder");
    }
}
