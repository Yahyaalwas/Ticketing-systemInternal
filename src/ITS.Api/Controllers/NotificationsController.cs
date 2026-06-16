using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController(
    ApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponse>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == currentUser.UserId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync(cancellationToken);
        var unreadCount = await db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientUserId == currentUser.UserId && !n.IsRead, cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id, n.NotificationType.ToString(), n.Title, n.Body,
                n.TicketId, n.ProjectId, n.SourceUserId,
                n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(new NotificationListResponse(items, total, unreadCount, page, pageSize));
    }

    [HttpPost("{notificationId:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(long notificationId, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == currentUser.UserId, cancellationToken);

        if (notification is null) return NotFound();

        notification.MarkRead(dateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var unread = await db.Notifications
            .Where(n => n.RecipientUserId == currentUser.UserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        var now = dateTime.UtcNow;
        foreach (var n in unread)
            n.MarkRead(now);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record NotificationDto(
    long Id, string Type, string Title, string? Body,
    Guid? TicketId, Guid? ProjectId, Guid? SourceUserId,
    bool IsRead, DateTime? ReadAt, DateTime CreatedAt);

public sealed record NotificationListResponse(
    IReadOnlyList<NotificationDto> Items, int TotalCount,
    int UnreadCount, int Page, int PageSize);
