using ITS.Domain.Common;

namespace ITS.Domain.Entities.Content;

public class CommentHistory : BaseEntity<long>
{
    private CommentHistory() { }

    internal static CommentHistory Create(Guid commentId, string previousBody, Guid editedByUserId)
    {
        return new CommentHistory
        {
            CommentId = commentId,
            Body = previousBody,
            EditedAt = DateTime.UtcNow,
            EditedByUserId = editedByUserId
        };
    }

    public Guid CommentId { get; private set; }
    public string Body { get; private set; } = default!;
    public DateTime EditedAt { get; private set; }
    public Guid EditedByUserId { get; private set; }
}
