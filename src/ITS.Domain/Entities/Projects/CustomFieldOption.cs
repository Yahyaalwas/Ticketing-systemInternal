using ITS.Domain.Common;

namespace ITS.Domain.Entities.Projects;

public class CustomFieldOption : BaseEntity<int>
{
    private CustomFieldOption() { }

    public static CustomFieldOption Create(int customFieldId, string value, int displayOrder)
    {
        return new CustomFieldOption
        {
            CustomFieldId = customFieldId,
            Value = value,
            DisplayOrder = displayOrder,
            IsActive = true
        };
    }

    public int CustomFieldId { get; private set; }
    public string Value { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string value, int displayOrder)
    {
        Value = value;
        DisplayOrder = displayOrder;
    }
}
