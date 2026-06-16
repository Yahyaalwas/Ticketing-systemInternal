namespace ITS.Domain.Entities.Content;

public class CommentMention
{
    private CommentMention() { }

    internal static CommentMention Create(Guid commentId, Guid mentionedUserId)
        => new() { CommentId = commentId, MentionedUserId = mentionedUserId };

    public Guid CommentId { get; private set; }
    public Guid MentionedUserId { get; private set; }
}
