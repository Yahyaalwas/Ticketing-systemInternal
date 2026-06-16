using ITS.Domain.Common;
using ITS.Domain.DomainEvents.Comments;

namespace ITS.Domain.Entities.Content;

public class Comment : AuditableEntity<Guid>
{
    private Comment() { }

    public static Comment Create(Guid ticketId, Guid? parentCommentId, Guid authorUserId, string body, string? bodyHtml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        var comment = new Comment
        {
            Id = Guid.CreateVersion7(),
            TicketId = ticketId,
            ParentCommentId = parentCommentId,
            AuthorUserId = authorUserId,
            Body = body,
            BodyHtml = bodyHtml,
            IsEdited = false
        };

        comment.RaiseDomainEvent(new CommentAddedDomainEvent(comment.Id, ticketId, authorUserId));
        return comment;
    }

    public Guid TicketId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = default!;
    public string? BodyHtml { get; private set; }
    public bool IsEdited { get; private set; }

    // Navigation
    public ICollection<CommentHistory> History { get; private set; } = [];
    public ICollection<CommentMention> Mentions { get; private set; } = [];
    public ICollection<Attachment> Attachments { get; private set; } = [];

    public CommentHistory Edit(string newBody, string? newBodyHtml, Guid editedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newBody);

        var history = CommentHistory.Create(Id, Body, editedByUserId);
        History.Add(history);

        Body = newBody;
        BodyHtml = newBodyHtml;
        IsEdited = true;

        return history;
    }

    public CommentMention AddMention(Guid mentionedUserId)
    {
        if (Mentions.Any(m => m.MentionedUserId == mentionedUserId))
            return Mentions.First(m => m.MentionedUserId == mentionedUserId);

        var mention = CommentMention.Create(Id, mentionedUserId);
        Mentions.Add(mention);
        return mention;
    }
}
