using ITS.Domain.Entities.Identity;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("RoleId").UseIdentityColumn();

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Scope)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("UX_Roles_Name");
    }
}
