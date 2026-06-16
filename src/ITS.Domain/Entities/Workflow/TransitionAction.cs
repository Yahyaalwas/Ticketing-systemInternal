using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Workflow;

public class TransitionAction : BaseEntity<int>
{
    private TransitionAction() { }

    internal static TransitionAction Create(int transitionId, TransitionActionType actionType, string configuration, int displayOrder)
    {
        return new TransitionAction
        {
            TransitionId = transitionId,
            ActionType = actionType,
            Configuration = configuration,
            DisplayOrder = displayOrder
        };
    }

    public int TransitionId { get; private set; }
    public TransitionActionType ActionType { get; private set; }
    public string Configuration { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
}
