using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class CustomFieldOptionConfiguration : IEntityTypeConfiguration<CustomFieldOption>
{
    public void Configure(EntityTypeBuilder<CustomFieldOption> builder)
    {
        builder.ToTable("CustomFieldOptions", "projects");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("CustomFieldOptionId").UseIdentityColumn();

        builder.Property(o => o.Value).IsRequired().HasMaxLength(500);

        builder.HasIndex(o => new { o.CustomFieldId, o.DisplayOrder })
            .HasDatabaseName("IX_CustomFieldOptions_FieldId_DisplayOrder");
    }
}
