using FluentAssertions;
using ITS.Domain.DomainEvents.Tickets;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Exceptions;

namespace ITS.UnitTests.Domain;

public class TicketTests
{
    private static Ticket CreateSampleTicket(
        Guid? projectId = null,
        int ticketNumber = 1,
        string title = "Sample Ticket",
        Guid? reporterUserId = null)
    {
        return Ticket.Create(
            projectId ?? Guid.NewGuid(),
            ticketNumber,
            title,
            description: null,
            issueTypeId: 1,
            statusId: 1,
            priorityId: null,
            reporterUserId: reporterUserId ?? Guid.NewGuid(),
            assigneeUserId: null,
            dueDate: null,
            storyPoints: null,
            createdByUserId: Guid.NewGuid());
    }

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var projectId = Guid.NewGuid();
        const string title = "Login page crashes on IE11";

        var ticket = CreateSampleTicket(projectId: projectId, ticketNumber: 42, title: title);

        ticket.ProjectId.Should().Be(projectId);
        ticket.TicketNumber.Should().Be(42);
        ticket.Title.Should().Be(title);
        ticket.StatusId.Should().Be(1);
        ticket.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_RaisesTicketCreatedDomainEvent()
    {
        var ticket = CreateSampleTicket();

        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketCreatedDomainEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ThrowsArgumentException(string title)
    {
        var act = () => CreateSampleTicket(title: title);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Assign_WithNewAssignee_RaisesTicketAssignedDomainEvent()
    {
        var ticket = CreateSampleTicket();
        ticket.ClearDomainEvents();

        var newAssignee = Guid.NewGuid();
        ticket.Assign(newAssignee, Guid.NewGuid());

        ticket.AssigneeUserId.Should().Be(newAssignee);
        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketAssignedDomainEvent>();
    }

    [Fact]
    public void Assign_WithSameAssignee_DoesNotRaiseDomainEvent()
    {
        var assigneeId = Guid.NewGuid();
        var ticket = CreateSampleTicket();
        ticket.Assign(assigneeId, Guid.NewGuid());
        ticket.ClearDomainEvents();

        ticket.Assign(assigneeId, Guid.NewGuid()); // same assignee

        ticket.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SetParent_ToSelf_ThrowsDomainException()
    {
        var ticket = CreateSampleTicket();

        var act = () => ticket.SetParent(ticket.Id);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be its own parent*");
    }

    [Fact]
    public void SetParent_WhenEpicAlreadySet_ThrowsDomainException()
    {
        var ticket = CreateSampleTicket();
        ticket.SetEpic(Guid.NewGuid());

        var act = () => ticket.SetParent(Guid.NewGuid());

        act.Should().Throw<DomainException>()
            .WithMessage("*sub-task*epic link*");
    }

    [Fact]
    public void SetStoryPoints_WithNegativeValue_ThrowsDomainException()
    {
        var ticket = CreateSampleTicket();

        var act = () => ticket.SetStoryPoints(-1m);

        act.Should().Throw<DomainException>()
            .WithMessage("*Story points cannot be negative*");
    }

    [Fact]
    public void TransitionTo_SetsStatusAndRaisesDomainEvent()
    {
        var ticket = CreateSampleTicket();
        ticket.ClearDomainEvents();
        const int newStatusId = 2;

        ticket.TransitionTo(newStatusId, null, null, Guid.NewGuid());

        ticket.StatusId.Should().Be(newStatusId);
        ticket.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TicketStatusChangedDomainEvent>();
    }

    [Fact]
    public void TransitionTo_FinalStatus_SetsResolution()
    {
        var ticket = CreateSampleTicket();
        var resolvedAt = DateTime.UtcNow;

        ticket.TransitionTo(3, resolutionId: 1, resolvedAt: resolvedAt, transitionedByUserId: Guid.NewGuid());

        ticket.ResolutionId.Should().Be(1);
        ticket.ResolvedAt.Should().BeCloseTo(resolvedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SoftDelete_SetsIsDeletedTrue()
    {
        var ticket = CreateSampleTicket();
        var deletedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;

        ticket.SoftDelete(deletedBy, now);

        ticket.IsDeleted.Should().BeTrue();
        ticket.DeletedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        ticket.DeletedByUserId.Should().Be(deletedBy);
    }

    [Fact]
    public void AddWatcher_DuplicateUser_ThrowsDomainException()
    {
        var ticket = CreateSampleTicket();
        var userId = Guid.NewGuid();
        ticket.AddWatcher(userId);

        var act = () => ticket.AddWatcher(userId);

        act.Should().Throw<DomainException>()
            .WithMessage("*already watching*");
    }
}
