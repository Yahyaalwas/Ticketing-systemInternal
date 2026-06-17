using ITS.Domain.Entities.Projects;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinitions", "projects");
        builder.HasKey(cf => cf.Id);
        builder.Property(cf => cf.Id).HasColumnName("CustomFieldDefinitionId").UseIdentityColumn();

        builder.Property(cf => cf.Name).IsRequired().HasMaxLength(100);
        builder.Property(cf => cf.FieldType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(cf => new { cf.ProjectId, cf.Name })
            .IsUnique()
            .HasDatabaseName("UX_CustomFieldDefinitions_ProjectId_Name");

        builder.HasMany(cf => cf.Options)
            .WithOne()
            .HasForeignKey(o => o.CustomFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
