using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class IssueTypeConfiguration : IEntityTypeConfiguration<IssueType>
{
    public void Configure(EntityTypeBuilder<IssueType> builder)
    {
        builder.ToTable("IssueTypes", "projects");
        builder.HasKey(it => it.Id);
        builder.Property(it => it.Id).HasColumnName("IssueTypeId").UseIdentityColumn();

        builder.Property(it => it.Name).IsRequired().HasMaxLength(100);
        builder.Property(it => it.Description).HasMaxLength(500);
        builder.Property(it => it.IconUrl).HasMaxLength(512);

        builder.HasIndex(it => new { it.ProjectId, it.Name })
            .IsUnique()
            .HasDatabaseName("UX_IssueTypes_ProjectId_Name");

        builder.HasIndex(it => new { it.ProjectId, it.DisplayOrder })
            .HasDatabaseName("IX_IssueTypes_ProjectId_DisplayOrder");
    }
}
