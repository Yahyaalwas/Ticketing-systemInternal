using ITS.Domain.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Tickets;

public class ResolutionConfiguration : IEntityTypeConfiguration<Resolution>
{
    public void Configure(EntityTypeBuilder<Resolution> builder)
    {
        builder.ToTable("Resolutions", "tickets");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ResolutionId").UseIdentityColumn();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("UX_Resolutions_Name");

        builder.HasIndex(r => r.DisplayOrder)
            .HasDatabaseName("IX_Resolutions_DisplayOrder");
    }
}
