using ITS.Domain.Common;

namespace ITS.Domain.DomainEvents.Tickets;

public sealed record TicketCreatedDomainEvent(
    Guid TicketId,
    Guid ProjectId,
    int TicketNumber,
    Guid CreatedByUserId) : DomainEvent;
