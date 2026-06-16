using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Notifications;

public class NotificationPreference : BaseEntity<int>
{
    private NotificationPreference() { }

    public static NotificationPreference CreateDefault(Guid userId, NotificationType notificationType)
    {
        return new NotificationPreference
        {
            UserId = userId,
            NotificationType = notificationType,
            InAppEnabled = true,
            EmailEnabled = true,
            EmailDigestMode = EmailDigestMode.Immediate
        };
    }

    public Guid UserId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public EmailDigestMode EmailDigestMode { get; private set; }

    public void Update(bool inAppEnabled, bool emailEnabled, EmailDigestMode digestMode)
    {
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        EmailDigestMode = digestMode;
    }
}
