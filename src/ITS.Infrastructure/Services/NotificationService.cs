using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Notifications;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Services;

internal sealed class NotificationService(
    IApplicationDbContext db,
    IDateTimeService dateTime,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string? body,
        Guid? ticketId,
        Guid? projectId,
        Guid? sourceUserId,
        CancellationToken cancellationToken = default)
    {
        await NotifyManyAsync(
            [recipientUserId], type, title, body,
            ticketId, projectId, sourceUserId, cancellationToken);
    }

    public async Task NotifyManyAsync(
        IEnumerable<Guid> recipientUserIds,
        NotificationType type,
        string title,
        string? body,
        Guid? ticketId,
        Guid? projectId,
        Guid? sourceUserId,
        CancellationToken cancellationToken = default)
    {
        var recipients = recipientUserIds
            .Where(id => id != sourceUserId) // Don't notify the actor
            .Distinct()
            .ToList();

        if (recipients.Count == 0)
            return;

        // Load preferences and user emails in one query
        var userDetails = await db.Users
            .AsNoTracking()
            .Where(u => recipients.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.Id, u.Email, u.DisplayName })
            .ToListAsync(cancellationToken);

        if (userDetails.Count == 0)
            return;

        var userIds = userDetails.Select(u => u.Id).ToList();

        var preferences = await db.NotificationPreferences
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId) && p.NotificationType == type)
            .ToDictionaryAsync(p => p.UserId, cancellationToken);

        var emailsToQueue = new List<(string Email, string Name)>();

        foreach (var user in userDetails)
        {
            preferences.TryGetValue(user.Id, out var pref);

            // Default: in-app enabled, email enabled when no preference record exists
            var inApp = pref?.InAppEnabled ?? true;
            var email = pref?.EmailEnabled ?? true;

            if (inApp)
            {
                var notification = Notification.Create(
                    user.Id, type, title, body, ticketId, projectId, sourceUserId);
                db.Notifications.Add(notification);
            }

            if (email)
                emailsToQueue.Add((user.Email, user.DisplayName));
        }

        // Queue emails for background sending
        foreach (var (recipientEmail, recipientName) in emailsToQueue)
        {
            var htmlBody = BuildEmailHtml(title, body, ticketId, projectId);
            var plainText = body ?? title;

            var queued = EmailQueue.Create(
                recipientEmail, recipientName,
                title, htmlBody, plainText,
                notificationIds: null,
                scheduledAt: dateTime.UtcNow);

            db.EmailQueue.Add(queued);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist notifications for type {NotificationType}", type);
        }
    }

    private static string BuildEmailHtml(string title, string? body, Guid? ticketId, Guid? projectId)
    {
        var bodyContent = string.IsNullOrWhiteSpace(body) ? string.Empty : $"<p>{body}</p>";
        var ticketLink = ticketId.HasValue
            ? $"<p><a href=\"/tickets/{ticketId}\">View Ticket</a></p>"
            : string.Empty;

        return $"""
                <!DOCTYPE html>
                <html><body>
                <h2>{title}</h2>
                {bodyContent}
                {ticketLink}
                </body></html>
                """;
    }
}
