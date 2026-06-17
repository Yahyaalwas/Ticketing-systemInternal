using ITS.Domain.Entities.Workflow;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Workflow;

public class TransitionGuardConfiguration : IEntityTypeConfiguration<TransitionGuard>
{
    public void Configure(EntityTypeBuilder<TransitionGuard> builder)
    {
        builder.ToTable("TransitionGuards", "workflow");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("TransitionGuardId").UseIdentityColumn();

        builder.Property(g => g.GuardType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(g => g.Configuration).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasIndex(g => new { g.TransitionId, g.DisplayOrder })
            .HasDatabaseName("IX_TransitionGuards_TransitionId_DisplayOrder");
    }
}
