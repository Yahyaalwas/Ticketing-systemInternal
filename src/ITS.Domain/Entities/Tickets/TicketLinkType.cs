using ITS.Domain.Common;

namespace ITS.Domain.Entities.Tickets;

public class TicketLinkType : BaseEntity<int>
{
    private TicketLinkType() { }

    public string Name { get; private set; } = default!;
    public string InwardName { get; private set; } = default!;
    public string OutwardName { get; private set; } = default!;
}
