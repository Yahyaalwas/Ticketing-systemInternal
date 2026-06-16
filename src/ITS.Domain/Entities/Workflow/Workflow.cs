using ITS.Domain.Common;

namespace ITS.Domain.Entities.Workflow;

public class Workflow : AuditableEntity<Guid>
{
    private Workflow() { }

    public static Workflow Create(string name, string? description, bool isTemplate, Guid? projectId)
    {
        if (!isTemplate && projectId is null)
            throw new ArgumentException("Non-template workflows must be associated with a project.");

        if (isTemplate && projectId is not null)
            throw new ArgumentException("Template workflows cannot be associated with a project.");

        return new Workflow
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            IsTemplate = isTemplate,
            ProjectId = projectId,
            Version = 1
        };
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsTemplate { get; private set; }
    public Guid? ProjectId { get; private set; }
    public int Version { get; private set; }

    // Navigation
    public ICollection<WorkflowStatus> Statuses { get; private set; } = [];
    public ICollection<WorkflowTransition> Transitions { get; private set; } = [];

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        Version++;
    }

    public WorkflowStatus AddStatus(string name, string? description, ITS.Domain.Enums.StatusCategory category, string? color, int displayOrder, bool isInitial, bool isFinal)
    {
        var status = WorkflowStatus.Create(Id, name, description, category, color, displayOrder, isInitial, isFinal);
        Statuses.Add(status);
        return status;
    }

    public WorkflowTransition AddTransition(string name, int fromStatusId, int toStatusId, bool requiresComment, int displayOrder)
    {
        var transition = WorkflowTransition.Create(Id, name, fromStatusId, toStatusId, requiresComment, displayOrder);
        Transitions.Add(transition);
        Version++;
        return transition;
    }

    public bool CanTransition(int fromStatusId, int toStatusId)
        => Transitions.Any(t => t.FromStatusId == fromStatusId && t.ToStatusId == toStatusId);

    public WorkflowTransition? GetTransition(int fromStatusId, int toStatusId)
        => Transitions.FirstOrDefault(t => t.FromStatusId == fromStatusId && t.ToStatusId == toStatusId);

    public WorkflowStatus? GetInitialStatus()
        => Statuses.FirstOrDefault(s => s.IsInitial);
}
