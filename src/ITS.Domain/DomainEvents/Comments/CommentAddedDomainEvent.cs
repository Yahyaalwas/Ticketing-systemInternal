using ITS.Domain.Common;

namespace ITS.Domain.DomainEvents.Comments;

public sealed record CommentAddedDomainEvent(
    Guid CommentId,
    Guid TicketId,
    Guid AuthorUserId) : DomainEvent;
