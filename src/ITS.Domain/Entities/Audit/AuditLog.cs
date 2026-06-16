using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Audit;

public class AuditLog : BaseEntity<long>
{
    private AuditLog() { }

    public static AuditLog Create(
        Guid? actorUserId,
        string? actorIpAddress,
        string? actorUserAgent,
        AuditOperation operation,
        string entityType,
        string entityId,
        Guid? projectId,
        string? beforeState,
        string? afterState,
        bool succeeded,
        string? failureReason,
        string? correlationId)
    {
        return new AuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorIpAddress = actorIpAddress,
            ActorUserAgent = actorUserAgent,
            Operation = operation,
            EntityType = entityType,
            EntityId = entityId,
            ProjectId = projectId,
            BeforeState = beforeState,
            AfterState = afterState,
            Succeeded = succeeded,
            FailureReason = failureReason,
            CorrelationId = correlationId
        };
    }

    public DateTime OccurredAt { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorIpAddress { get; private set; }
    public string? ActorUserAgent { get; private set; }
    public AuditOperation Operation { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public Guid? ProjectId { get; private set; }
    public string? BeforeState { get; private set; }
    public string? AfterState { get; private set; }
    public bool Succeeded { get; private set; }
    public string? FailureReason { get; private set; }
    public string? CorrelationId { get; private set; }
}
