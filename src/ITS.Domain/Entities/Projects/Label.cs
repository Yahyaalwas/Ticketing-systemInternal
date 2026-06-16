using ITS.Domain.Common;

namespace ITS.Domain.Entities.Projects;

public class Label : BaseEntity<int>
{
    private Label() { }

    public static Label CreateGlobal(string name, string? color, string? description)
    {
        return new Label
        {
            ProjectId = null,
            Name = name,
            Color = color,
            Description = description,
            IsActive = true
        };
    }

    public static Label CreateProjectScoped(Guid projectId, string name, string? color, string? description)
    {
        return new Label
        {
            ProjectId = projectId,
            Name = name,
            Color = color,
            Description = description,
            IsActive = true
        };
    }

    public Guid? ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Color { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsGlobal => ProjectId is null;

    public void Update(string name, string? color, string? description)
    {
        Name = name;
        Color = color;
        Description = description;
    }
}
