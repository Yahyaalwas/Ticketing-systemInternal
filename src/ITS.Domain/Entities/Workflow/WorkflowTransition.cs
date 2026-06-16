using ITS.Domain.Common;

namespace ITS.Domain.Entities.Workflow;

public class WorkflowTransition : BaseEntity<int>
{
    private WorkflowTransition() { }

    internal static WorkflowTransition Create(
        Guid workflowId,
        string name,
        int fromStatusId,
        int toStatusId,
        bool requiresComment,
        int displayOrder)
    {
        return new WorkflowTransition
        {
            WorkflowId = workflowId,
            Name = name,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            RequiresComment = requiresComment,
            DisplayOrder = displayOrder
        };
    }

    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = default!;
    public int FromStatusId { get; private set; }
    public int ToStatusId { get; private set; }
    public bool RequiresComment { get; private set; }
    public int DisplayOrder { get; private set; }

    // Navigation
    public WorkflowStatus FromStatus { get; private set; } = default!;
    public WorkflowStatus ToStatus { get; private set; } = default!;
    public ICollection<TransitionGuard> Guards { get; private set; } = [];
    public ICollection<TransitionAction> Actions { get; private set; } = [];

    public void AddGuard(ITS.Domain.Enums.GuardType guardType, string configuration)
    {
        var guard = TransitionGuard.Create(Id, guardType, configuration, Guards.Count);
        Guards.Add(guard);
    }

    public void AddAction(ITS.Domain.Enums.TransitionActionType actionType, string configuration)
    {
        var action = TransitionAction.Create(Id, actionType, configuration, Actions.Count);
        Actions.Add(action);
    }
}
