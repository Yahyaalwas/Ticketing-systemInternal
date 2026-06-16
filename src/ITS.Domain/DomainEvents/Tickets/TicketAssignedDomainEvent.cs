using ITS.Domain.Common;

namespace ITS.Domain.DomainEvents.Tickets;

public sealed record TicketAssignedDomainEvent(
    Guid TicketId,
    Guid ProjectId,
    int TicketNumber,
    Guid AssigneeUserId,
    Guid AssignedByUserId) : DomainEvent;
