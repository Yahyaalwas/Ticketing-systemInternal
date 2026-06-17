using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class PriorityConfiguration : IEntityTypeConfiguration<Priority>
{
    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.ToTable("Priorities", "projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("PriorityId").UseIdentityColumn();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.IconUrl).HasMaxLength(512);
        builder.Property(p => p.Color).HasMaxLength(20);

        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("UX_Priorities_Name");

        builder.HasIndex(p => p.DisplayOrder)
            .HasDatabaseName("IX_Priorities_DisplayOrder");
    }
}
