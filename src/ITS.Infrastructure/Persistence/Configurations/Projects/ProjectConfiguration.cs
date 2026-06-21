using ITS.Domain.Entities.Identity;
using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEntity = ITS.Domain.Entities.Workflow.Workflow;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", "projects");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ProjectId")
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(p => p.ProjectKey).IsRequired().HasMaxLength(10);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.AvatarUrl).HasMaxLength(512);

        builder.Property(p => p.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(p => p.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(p => p.ArchivedAt).HasColumnType("datetime2(7)");
        builder.Property(p => p.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(p => p.ProjectKey)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Projects_ProjectKey");

        builder.HasIndex(p => p.DepartmentId).HasDatabaseName("IX_Projects_DepartmentId");
        builder.HasIndex(p => p.LeadUserId).HasDatabaseName("IX_Projects_LeadUserId");

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.IssueTypes)
            .WithOne()
            .HasForeignKey(it => it.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Labels)
            .WithOne()
            .HasForeignKey(l => l.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.CustomFields)
            .WithOne()
            .HasForeignKey(cf => cf.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.LeadUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<WorkflowEntity>()
            .WithMany()
            .HasForeignKey(p => p.ActiveWorkflowId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
