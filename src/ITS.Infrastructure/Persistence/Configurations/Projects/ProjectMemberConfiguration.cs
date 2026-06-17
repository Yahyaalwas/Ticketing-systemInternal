using ITS.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Projects;

public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers", "projects");
        builder.HasKey(pm => pm.Id);
        builder.Property(pm => pm.Id).HasColumnName("ProjectMemberId").HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(pm => pm.GrantedAt).HasColumnType("datetime2(7)").IsRequired();

        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique()
            .HasDatabaseName("UX_ProjectMembers_ProjectId_UserId");

        builder.HasIndex(pm => pm.UserId)
            .HasDatabaseName("IX_ProjectMembers_UserId");

        // ProjectId → Projects cascade is configured on Project side
        // Add UserId and RoleId FKs here
        builder.HasOne<ITS.Domain.Entities.Identity.User>()
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ITS.Domain.Entities.Identity.Role>()
            .WithMany()
            .HasForeignKey(pm => pm.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
