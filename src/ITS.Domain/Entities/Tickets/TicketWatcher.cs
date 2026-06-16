namespace ITS.Domain.Entities.Tickets;

public class TicketWatcher
{
    private TicketWatcher() { }

    internal static TicketWatcher Create(Guid ticketId, Guid userId)
        => new() { TicketId = ticketId, UserId = userId, AddedAt = DateTime.UtcNow };

    public Guid TicketId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime AddedAt { get; private set; }
}
