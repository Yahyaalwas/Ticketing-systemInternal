using ITS.Domain.Common;

namespace ITS.Domain.Entities.Projects;

public class IssueType : BaseEntity<int>
{
    private IssueType() { }

    public static IssueType Create(Guid projectId, string name, string? description, string? iconUrl, bool isSubTask, bool isEpic)
    {
        if (isSubTask && isEpic)
            throw new ArgumentException("An issue type cannot be both a sub-task and an epic.");

        return new IssueType
        {
            ProjectId = projectId,
            Name = name,
            Description = description,
            IconUrl = iconUrl,
            IsSubTask = isSubTask,
            IsEpic = isEpic,
            IsActive = true
        };
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsSubTask { get; private set; }
    public bool IsEpic { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? description, string? iconUrl, int displayOrder)
    {
        Name = name;
        Description = description;
        IconUrl = iconUrl;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
