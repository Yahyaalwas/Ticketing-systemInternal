using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Notifications;

public class EmailQueue : BaseEntity<long>
{
    private EmailQueue() { }

    public static EmailQueue Create(
        string recipientEmail,
        string? recipientName,
        string subject,
        string htmlBody,
        string? plainTextBody,
        string? notificationIds,
        DateTime? scheduledAt = null)
    {
        return new EmailQueue
        {
            RecipientEmail = recipientEmail,
            RecipientName = recipientName,
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody,
            NotificationIds = notificationIds,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            Status = EmailStatus.Pending,
            AttemptCount = 0
        };
    }

    public string RecipientEmail { get; private set; } = default!;
    public string? RecipientName { get; private set; }
    public string Subject { get; private set; } = default!;
    public string HtmlBody { get; private set; } = default!;
    public string? PlainTextBody { get; private set; }
    public string? NotificationIds { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public EmailStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkSending()
    {
        Status = EmailStatus.Sending;
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
    }

    public void MarkSent()
    {
        Status = EmailStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = EmailStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void ResetToPending()
        => Status = EmailStatus.Pending;
}
