using ITS.Domain.Enums;

namespace ITS.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        AuditOperation operation,
        string entityType,
        string entityId,
        Guid? projectId = null,
        string? beforeState = null,
        string? afterState = null,
        bool succeeded = true,
        string? failureReason = null,
        CancellationToken cancellationToken = default);
}
