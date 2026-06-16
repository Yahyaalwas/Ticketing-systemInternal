using ITS.Domain.Common;

namespace ITS.Domain.Entities.Tickets;

public class TicketLink : BaseEntity<Guid>
{
    private TicketLink() { }

    public static TicketLink Create(Guid sourceTicketId, Guid targetTicketId, int linkTypeId, Guid createdByUserId)
    {
        if (sourceTicketId == targetTicketId)
            throw new ArgumentException("A ticket cannot be linked to itself.");

        return new TicketLink
        {
            Id = Guid.CreateVersion7(),
            SourceTicketId = sourceTicketId,
            TargetTicketId = targetTicketId,
            LinkTypeId = linkTypeId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };
    }

    public Guid SourceTicketId { get; private set; }
    public Guid TargetTicketId { get; private set; }
    public int LinkTypeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    // Navigation
    public TicketLinkType LinkType { get; private set; } = default!;
}
