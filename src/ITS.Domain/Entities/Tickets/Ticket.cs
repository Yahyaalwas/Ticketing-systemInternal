using ITS.Domain.Common;
using ITS.Domain.DomainEvents.Tickets;
using ITS.Domain.Entities.Content;
using ITS.Domain.Exceptions;

namespace ITS.Domain.Entities.Tickets;

public class Ticket : AuditableEntity<Guid>
{
    private Ticket() { }

    public static Ticket Create(
        Guid projectId,
        int ticketNumber,
        string title,
        string? description,
        int issueTypeId,
        int statusId,
        int? priorityId,
        Guid reporterUserId,
        Guid? assigneeUserId,
        DateOnly? dueDate,
        decimal? storyPoints,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var ticket = new Ticket
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            TicketNumber = ticketNumber,
            Title = title,
            Description = description,
            IssueTypeId = issueTypeId,
            StatusId = statusId,
            PriorityId = priorityId,
            ReporterUserId = reporterUserId,
            AssigneeUserId = assigneeUserId,
            DueDate = dueDate,
            StoryPoints = storyPoints
        };

        ticket.RaiseDomainEvent(new TicketCreatedDomainEvent(ticket.Id, projectId, ticketNumber, createdByUserId));

        return ticket;
    }

    public Guid ProjectId { get; private set; }
    public int TicketNumber { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? DescriptionHtml { get; private set; }
    public int IssueTypeId { get; private set; }
    public int StatusId { get; private set; }
    public int? PriorityId { get; private set; }
    public Guid? AssigneeUserId { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public Guid? ParentTicketId { get; private set; }
    public Guid? EpicTicketId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public decimal? StoryPoints { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public decimal? ActualHours { get; private set; }
    public int? ResolutionId { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? SlaBreachAt { get; private set; }

    // Navigation
    public ICollection<TicketLabel> Labels { get; private set; } = [];
    public ICollection<TicketLink> OutboundLinks { get; private set; } = [];
    public ICollection<TicketLink> InboundLinks { get; private set; } = [];
    public ICollection<TicketWatcher> Watchers { get; private set; } = [];
    public ICollection<CustomFieldValue> CustomFieldValues { get; private set; } = [];
    public ICollection<Comment> Comments { get; private set; } = [];
    public ICollection<Attachment> Attachments { get; private set; } = [];

    public void UpdateTitle(string title, Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
    }

    public void UpdateDescription(string? description, string? descriptionHtml)
    {
        Description = description;
        DescriptionHtml = descriptionHtml;
    }

    public void Assign(Guid? assigneeUserId, Guid updatedByUserId)
    {
        var previous = AssigneeUserId;
        AssigneeUserId = assigneeUserId;

        if (previous != assigneeUserId && assigneeUserId.HasValue)
            RaiseDomainEvent(new TicketAssignedDomainEvent(Id, ProjectId, TicketNumber, assigneeUserId.Value, updatedByUserId));
    }

    public void ChangePriority(int? priorityId)
        => PriorityId = priorityId;

    public void SetDueDate(DateOnly? dueDate)
        => DueDate = dueDate;

    public void SetStoryPoints(decimal? storyPoints)
    {
        if (storyPoints.HasValue && storyPoints < 0)
            throw new DomainException("Story points cannot be negative.");
        StoryPoints = storyPoints;
    }

    public void SetEstimatedHours(decimal? hours)
    {
        if (hours.HasValue && hours < 0)
            throw new DomainException("Estimated hours cannot be negative.");
        EstimatedHours = hours;
    }

    public void LogActualHours(decimal? hours)
    {
        if (hours.HasValue && hours < 0)
            throw new DomainException("Actual hours cannot be negative.");
        ActualHours = hours;
    }

    public void TransitionTo(int newStatusId, int? resolutionId, DateTime? resolvedAt, Guid transitionedByUserId)
    {
        var previousStatusId = StatusId;
        StatusId = newStatusId;

        if (resolutionId.HasValue)
        {
            ResolutionId = resolutionId;
            ResolvedAt = resolvedAt ?? DateTime.UtcNow;
        }
        else
        {
            ResolutionId = null;
            ResolvedAt = null;
        }

        RaiseDomainEvent(new TicketStatusChangedDomainEvent(Id, ProjectId, TicketNumber, previousStatusId, newStatusId, transitionedByUserId));
    }

    public void SetSlaBreachAt(DateTime? slaBreachAt)
        => SlaBreachAt = slaBreachAt;

    public void SetParent(Guid? parentTicketId)
    {
        if (parentTicketId == Id)
            throw new DomainException("A ticket cannot be its own parent.");
        if (parentTicketId.HasValue && EpicTicketId.HasValue)
            throw new DomainException("A sub-task cannot simultaneously have an epic link. Set the epic on the parent ticket.");
        ParentTicketId = parentTicketId;
    }

    public void SetEpic(Guid? epicTicketId)
    {
        if (epicTicketId == Id)
            throw new DomainException("A ticket cannot be its own epic.");
        if (epicTicketId.HasValue && ParentTicketId.HasValue)
            throw new DomainException("A sub-task inherits its parent's epic. Remove the parent relationship first.");
        EpicTicketId = epicTicketId;
    }

    public TicketWatcher AddWatcher(Guid userId)
    {
        if (Watchers.Any(w => w.UserId == userId))
            throw new DomainException($"User {userId} is already watching this ticket.");

        var watcher = TicketWatcher.Create(Id, userId);
        Watchers.Add(watcher);
        return watcher;
    }

    public void RemoveWatcher(Guid userId)
    {
        var watcher = Watchers.FirstOrDefault(w => w.UserId == userId)
            ?? throw new DomainException($"User {userId} is not watching this ticket.");
        Watchers.Remove(watcher);
    }
}
