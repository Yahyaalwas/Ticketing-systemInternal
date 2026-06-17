using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Identity;

public class UserGlobalRoleConfiguration : IEntityTypeConfiguration<UserGlobalRole>
{
    public void Configure(EntityTypeBuilder<UserGlobalRole> builder)
    {
        builder.ToTable("UserGlobalRoles", "identity");
        builder.HasKey(ugr => ugr.Id);
        builder.Property(ugr => ugr.Id).HasColumnName("UserGlobalRoleId").UseIdentityColumn();

        builder.Property(ugr => ugr.GrantedAt).HasColumnType("datetime2(7)").IsRequired();

        builder.HasIndex(ugr => new { ugr.UserId, ugr.RoleId })
            .IsUnique()
            .HasDatabaseName("UX_UserGlobalRoles_UserId_RoleId");

        builder.HasIndex(ugr => ugr.UserId)
            .HasDatabaseName("IX_UserGlobalRoles_UserId");

        builder.HasOne(ugr => ugr.User)
            .WithMany(u => u.GlobalRoles)
            .HasForeignKey(ugr => ugr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ugr => ugr.Role)
            .WithMany()
            .HasForeignKey(ugr => ugr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
