using ITS.Domain.Entities.Notifications;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations.Notifications;

public class EmailQueueConfiguration : IEntityTypeConfiguration<EmailQueue>
{
    public void Configure(EntityTypeBuilder<EmailQueue> builder)
    {
        builder.ToTable("EmailQueue", "notifications");
        builder.HasKey(eq => eq.Id);
        builder.Property(eq => eq.Id).HasColumnName("EmailQueueId").UseIdentityColumn<long>();

        builder.Property(eq => eq.RecipientEmail).IsRequired().HasMaxLength(256);
        builder.Property(eq => eq.RecipientName).HasMaxLength(256);
        builder.Property(eq => eq.Subject).IsRequired().HasMaxLength(512);
        builder.Property(eq => eq.HtmlBody).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(eq => eq.PlainTextBody).HasColumnType("nvarchar(max)");
        builder.Property(eq => eq.NotificationIds).HasMaxLength(2000);
        builder.Property(eq => eq.ErrorMessage).HasMaxLength(2000);

        builder.Property(eq => eq.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(eq => eq.ScheduledAt).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(eq => eq.SentAt).HasColumnType("datetime2(7)");
        builder.Property(eq => eq.LastAttemptAt).HasColumnType("datetime2(7)");

        // Queue processing index — pick up pending/retryable emails in schedule order
        builder.HasIndex(eq => new { eq.Status, eq.ScheduledAt })
            .HasDatabaseName("IX_EmailQueue_Status_ScheduledAt");
    }
}
