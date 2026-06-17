using ITS.Domain.Entities.Notifications;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences", "notifications");
        builder.HasKey(np => np.Id);
        builder.Property(np => np.Id).HasColumnName("NotificationPreferenceId").UseIdentityColumn();

        builder.Property(np => np.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(np => np.EmailDigestMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(np => new { np.UserId, np.NotificationType })
            .IsUnique()
            .HasDatabaseName("UX_NotificationPreferences_UserId_Type");

        builder.HasOne<ITS.Domain.Entities.Identity.User>()
            .WithMany()
            .HasForeignKey(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
