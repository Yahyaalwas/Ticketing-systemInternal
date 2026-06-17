using ITS.Application.Common.Models.Ai;

namespace ITS.Application.Common.Interfaces;

public interface IAiAuditService
{
    Task LogAsync(AiAuditEntry entry, CancellationToken ct = default);
}
