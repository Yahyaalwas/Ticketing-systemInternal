using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Projects;

public class CustomFieldDefinition : BaseEntity<int>
{
    private CustomFieldDefinition() { }

    public static CustomFieldDefinition Create(Guid projectId, string name, CustomFieldType fieldType, bool isRequired, int displayOrder)
    {
        return new CustomFieldDefinition
        {
            ProjectId = projectId,
            Name = name,
            FieldType = fieldType,
            IsRequired = isRequired,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = default!;
    public CustomFieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public ICollection<CustomFieldOption> Options { get; private set; } = [];

    public void Update(string name, bool isRequired, int displayOrder)
    {
        Name = name;
        IsRequired = isRequired;
        DisplayOrder = displayOrder;
    }

    public void Deactivate() => IsActive = false;
}
