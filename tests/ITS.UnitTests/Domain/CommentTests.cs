using FluentAssertions;
using ITS.Domain.DomainEvents.Comments;
using ITS.Domain.Entities.Content;

namespace ITS.UnitTests.Domain;

public class CommentTests
{
    [Fact]
    public void Create_WithValidData_RaisesCommentAddedDomainEvent()
    {
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = Comment.Create(ticketId, null, authorId, "This is a comment.", "<p>This is a comment.</p>");

        comment.TicketId.Should().Be(ticketId);
        comment.AuthorUserId.Should().Be(authorId);
        comment.IsEdited.Should().BeFalse();
        comment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CommentAddedDomainEvent>();
    }

    [Fact]
    public void Edit_StoresOldBodyInHistory()
    {
        var comment = Comment.Create(Guid.NewGuid(), null, Guid.NewGuid(), "Original body.", null);
        comment.ClearDomainEvents();

        var history = comment.Edit("Updated body.", null, Guid.NewGuid());

        comment.Body.Should().Be("Updated body.");
        comment.IsEdited.Should().BeTrue();
        history.Body.Should().Be("Original body.");
        comment.History.Should().Contain(history);
    }

    [Fact]
    public void AddMention_SameTwice_AddedOnce()
    {
        var comment = Comment.Create(Guid.NewGuid(), null, Guid.NewGuid(), "Hello @user.", null);
        var userId = Guid.NewGuid();

        comment.AddMention(userId);
        comment.AddMention(userId); // idempotent

        comment.Mentions.Should().ContainSingle(m => m.MentionedUserId == userId);
    }

    [Fact]
    public void Create_WithEmptyBody_ThrowsArgumentException()
    {
        var act = () => Comment.Create(Guid.NewGuid(), null, Guid.NewGuid(), "", null);

        act.Should().Throw<ArgumentException>();
    }
}
