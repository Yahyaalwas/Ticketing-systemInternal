using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;

namespace ITS.Infrastructure.Services;

internal sealed class AuditService : IAuditService
{
    public Task LogAsync(AuditOperation operation, string entityType, string entityId,
        Guid? projectId = null, string? beforeState = null, string? afterState = null,
        bool succeeded = true, string? failureReason = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
