using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;

namespace ITS.Infrastructure.Services;

internal sealed class NotificationService : INotificationService
{
    public Task NotifyAsync(Guid recipientUserId, NotificationType type, string title, string? body,
        Guid? ticketId, Guid? projectId, Guid? sourceUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyManyAsync(IEnumerable<Guid> recipientUserIds, NotificationType type, string title, string? body,
        Guid? ticketId, Guid? projectId, Guid? sourceUserId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
