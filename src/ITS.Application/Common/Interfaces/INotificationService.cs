using ITS.Domain.Enums;

namespace ITS.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string? body,
        Guid? ticketId,
        Guid? projectId,
        Guid? sourceUserId,
        CancellationToken cancellationToken = default);

    Task NotifyManyAsync(
        IEnumerable<Guid> recipientUserIds,
        NotificationType type,
        string title,
        string? body,
        Guid? ticketId,
        Guid? projectId,
        Guid? sourceUserId,
        CancellationToken cancellationToken = default);
}
