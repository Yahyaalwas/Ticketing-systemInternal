using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Workflow;

public class WorkflowStatus : BaseEntity<int>
{
    private WorkflowStatus() { }

    internal static WorkflowStatus Create(
        Guid workflowId,
        string name,
        string? description,
        StatusCategory category,
        string? color,
        int displayOrder,
        bool isInitial,
        bool isFinal)
    {
        return new WorkflowStatus
        {
            WorkflowId = workflowId,
            Name = name,
            Description = description,
            Category = category,
            Color = color,
            DisplayOrder = displayOrder,
            IsInitial = isInitial,
            IsFinal = isFinal
        };
    }

    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public StatusCategory Category { get; private set; }
    public string? Color { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsInitial { get; private set; }
    public bool IsFinal { get; private set; }

    public void Update(string name, string? description, StatusCategory category, string? color, int displayOrder)
    {
        Name = name;
        Description = description;
        Category = category;
        Color = color;
        DisplayOrder = displayOrder;
    }
}
