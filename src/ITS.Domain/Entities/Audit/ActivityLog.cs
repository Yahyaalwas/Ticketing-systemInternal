using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Audit;

public class ActivityLog : BaseEntity<long>
{
    private ActivityLog() { }

    public static ActivityLog Create(
        Guid ticketId,
        Guid projectId,
        Guid actorUserId,
        ActivityType activityType,
        string entityType,
        string entityId,
        string? fieldName,
        string? oldValue,
        string? newValue)
    {
        return new ActivityLog
        {
            TicketId = ticketId,
            ProjectId = projectId,
            ActorUserId = actorUserId,
            ActivityType = activityType,
            EntityType = entityType,
            EntityId = entityId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            OccurredAt = DateTime.UtcNow
        };
    }

    public Guid TicketId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public ActivityType ActivityType { get; private set; }
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string? FieldName { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTime OccurredAt { get; private set; }
}
