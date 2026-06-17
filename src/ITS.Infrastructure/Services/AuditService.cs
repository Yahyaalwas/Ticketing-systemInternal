using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Services;

internal sealed class AuditService(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService dateTime,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(
        AuditOperation operation,
        string entityType,
        string entityId,
        Guid? projectId = null,
        string? beforeState = null,
        string? afterState = null,
        bool succeeded = true,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = AuditLog.Create(
                actorUserId: currentUser.IsAuthenticated ? currentUser.UserId : null,
                actorIpAddress: currentUser.IpAddress,
                actorUserAgent: currentUser.UserAgent,
                operation: operation,
                entityType: entityType,
                entityId: entityId,
                projectId: projectId,
                beforeState: beforeState,
                afterState: afterState,
                succeeded: succeeded,
                failureReason: failureReason,
                correlationId: null);

            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit failures must never surface to the caller — log and continue.
            logger.LogError(ex,
                "Failed to write audit log for operation {Operation} on {EntityType}/{EntityId}",
                operation, entityType, entityId);
        }
    }
}
