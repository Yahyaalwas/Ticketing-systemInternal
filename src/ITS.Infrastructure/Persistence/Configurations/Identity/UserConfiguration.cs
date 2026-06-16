using ITS.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("UserId")
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(u => u.AdObjectId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.UserPrincipalName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.FirstName).HasMaxLength(128);
        builder.Property(u => u.LastName).HasMaxLength(128);
        builder.Property(u => u.EmployeeId).HasMaxLength(64);
        builder.Property(u => u.JobTitle).HasMaxLength(256);
        builder.Property(u => u.PhoneNumber).HasMaxLength(50);
        builder.Property(u => u.AvatarUrl).HasMaxLength(512);

        builder.Property(u => u.TimeZoneId)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValue("UTC");

        builder.Property(u => u.Locale)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        builder.Property(u => u.CreatedAt)
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.UpdatedAt)
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(u => u.DeletedAt).HasColumnType("datetime2(7)");
        builder.Property(u => u.LastLoginAt).HasColumnType("datetime2(7)");
        builder.Property(u => u.LastAdSyncAt).HasColumnType("datetime2(7)");

        builder.Property(u => u.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Unique indexes
        builder.HasIndex(u => u.AdObjectId)
            .IsUnique()
            .HasDatabaseName("UX_Users_AdObjectId");

        builder.HasIndex(u => u.UserPrincipalName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Users_UserPrincipalName");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Users_Email");

        // Query indexes
        builder.HasIndex(u => new { u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_Users_IsActive_IsDeleted");

        builder.HasIndex(u => u.DisplayName)
            .HasDatabaseName("IX_Users_DisplayName");

        builder.HasIndex(u => u.DepartmentId)
            .HasDatabaseName("IX_Users_DepartmentId");

        // Relationships
        builder.HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Manager)
            .WithMany()
            .HasForeignKey(u => u.ManagerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
