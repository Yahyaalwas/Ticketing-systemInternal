namespace ITS.Domain.Entities.Tickets;

public class TicketLabel
{
    private TicketLabel() { }

    public static TicketLabel Create(Guid ticketId, int labelId)
        => new() { TicketId = ticketId, LabelId = labelId };

    public Guid TicketId { get; private set; }
    public int LabelId { get; private set; }
}
