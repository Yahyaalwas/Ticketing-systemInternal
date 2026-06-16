using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Notifications;

public class Notification : BaseEntity<long>
{
    private Notification() { }

    public static Notification Create(
        Guid recipientUserId,
        NotificationType notificationType,
        string title,
        string? body,
        Guid? ticketId,
        Guid? projectId,
        Guid? sourceUserId)
    {
        return new Notification
        {
            RecipientUserId = recipientUserId,
            NotificationType = notificationType,
            Title = title,
            Body = body,
            TicketId = ticketId,
            ProjectId = projectId,
            SourceUserId = sourceUserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public Guid RecipientUserId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public Guid? TicketId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid? SourceUserId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void MarkRead(DateTime readAt)
    {
        IsRead = true;
        ReadAt = readAt;
    }
}
