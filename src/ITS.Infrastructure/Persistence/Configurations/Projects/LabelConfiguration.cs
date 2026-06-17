using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("Labels", "projects");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("LabelId").UseIdentityColumn();

        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Color).HasMaxLength(20);
        builder.Property(l => l.Description).HasMaxLength(500);

        // Unique name per project scope (global labels are unique globally, project labels unique within project)
        builder.HasIndex(l => new { l.ProjectId, l.Name })
            .IsUnique()
            .HasDatabaseName("UX_Labels_ProjectId_Name");
    }
}
