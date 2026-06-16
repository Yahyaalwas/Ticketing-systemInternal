using ITS.Domain.Common;

namespace ITS.Domain.DomainEvents.Tickets;

public sealed record TicketStatusChangedDomainEvent(
    Guid TicketId,
    Guid ProjectId,
    int TicketNumber,
    int FromStatusId,
    int ToStatusId,
    Guid ChangedByUserId) : DomainEvent;
