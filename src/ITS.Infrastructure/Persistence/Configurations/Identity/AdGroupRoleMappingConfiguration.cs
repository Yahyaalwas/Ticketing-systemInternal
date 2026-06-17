using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Identity;

public class AdGroupRoleMappingConfiguration : IEntityTypeConfiguration<AdGroupRoleMapping>
{
    public void Configure(EntityTypeBuilder<AdGroupRoleMapping> builder)
    {
        builder.ToTable("AdGroupRoleMappings", "identity");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("AdGroupRoleMappingId").UseIdentityColumn();

        builder.Property(m => m.AdGroupDistinguishedName).IsRequired().HasMaxLength(512);
        builder.Property(m => m.AdGroupName).IsRequired().HasMaxLength(256);

        builder.HasIndex(m => new { m.AdGroupDistinguishedName, m.RoleId, m.ProjectId })
            .IsUnique()
            .HasDatabaseName("UX_AdGroupRoleMappings_Group_Role_Project");

        builder.HasIndex(m => m.AdGroupDistinguishedName)
            .HasDatabaseName("IX_AdGroupRoleMappings_AdGroupDn");

        builder.HasOne(m => m.Role)
            .WithMany()
            .HasForeignKey(m => m.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
