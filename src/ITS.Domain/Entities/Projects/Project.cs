using ITS.Domain.Common;
using ITS.Domain.DomainEvents.Projects;
using ITS.Domain.Exceptions;

namespace ITS.Domain.Entities.Projects;

public class Project : AuditableEntity<Guid>
{
    private Project() { }

    public static Project Create(
        string projectKey,
        string name,
        string? description,
        Guid leadUserId,
        int departmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var project = new Project
        {
            Id = Guid.CreateVersion7(),
            ProjectKey = projectKey.ToUpperInvariant(),
            Name = name,
            Description = description,
            LeadUserId = leadUserId,
            DepartmentId = departmentId,
            IsArchived = false
        };

        project.RaiseDomainEvent(new ProjectCreatedDomainEvent(project.Id, project.ProjectKey, project.Name));
        return project;
    }

    public string ProjectKey { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? AvatarUrl { get; private set; }
    public Guid LeadUserId { get; private set; }
    public int DepartmentId { get; private set; }
    public Guid? ActiveWorkflowId { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public Guid? ArchivedByUserId { get; private set; }

    // Navigation
    public ICollection<ProjectMember> Members { get; private set; } = [];
    public ICollection<IssueType> IssueTypes { get; private set; } = [];
    public ICollection<Label> Labels { get; private set; } = [];
    public ICollection<CustomFieldDefinition> CustomFields { get; private set; } = [];

    public void Update(string name, string? description, Guid leadUserId, string? avatarUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        LeadUserId = leadUserId;
        AvatarUrl = avatarUrl;
    }

    public void AssignWorkflow(Guid workflowId)
        => ActiveWorkflowId = workflowId;

    public void Archive(Guid archivedByUserId, DateTime utcNow)
    {
        if (IsArchived)
            throw new DomainException($"Project '{ProjectKey}' is already archived.");

        IsArchived = true;
        ArchivedAt = utcNow;
        ArchivedByUserId = archivedByUserId;
    }

    public void Unarchive()
    {
        IsArchived = false;
        ArchivedAt = null;
        ArchivedByUserId = null;
    }
}
