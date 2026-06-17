using ITS.Domain.Entities.Notifications;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("NotificationId").UseIdentityColumn<long>();

        builder.Property(n => n.Title).IsRequired().HasMaxLength(256);
        builder.Property(n => n.Body).HasColumnType("nvarchar(max)");
        builder.Property(n => n.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(n => n.CreatedAt).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(n => n.ReadAt).HasColumnType("datetime2(7)");

        // Primary delivery index — fetch unread notifications for a user, newest first
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead_CreatedAt");

        builder.HasOne<ITS.Domain.Entities.Identity.User>()
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ITS.Domain.Entities.Identity.User>()
            .WithMany()
            .HasForeignKey(n => n.SourceUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ITS.Domain.Entities.Tickets.Ticket>()
            .WithMany()
            .HasForeignKey(n => n.TicketId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
