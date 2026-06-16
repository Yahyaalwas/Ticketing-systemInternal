using ITS.Domain.Common;

namespace ITS.Domain.Entities.Projects;

public class Priority : BaseEntity<int>
{
    private Priority() { }

    public static Priority Create(string name, string? description, string? iconUrl, string? color, int displayOrder, int? slaTargetHours)
    {
        return new Priority
        {
            Name = name,
            Description = description,
            IconUrl = iconUrl,
            Color = color,
            DisplayOrder = displayOrder,
            SlaTargetHours = slaTargetHours,
            IsActive = true,
            IsSystemPriority = false
        };
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public string? Color { get; private set; }
    public int DisplayOrder { get; private set; }
    public int? SlaTargetHours { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystemPriority { get; private set; }

    public void Update(string name, string? description, string? color, int displayOrder, int? slaTargetHours)
    {
        Name = name;
        Description = description;
        Color = color;
        DisplayOrder = displayOrder;
        SlaTargetHours = slaTargetHours;
    }
}
