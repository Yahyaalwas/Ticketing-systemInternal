using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Workflow;

public class TransitionGuard : BaseEntity<int>
{
    private TransitionGuard() { }

    internal static TransitionGuard Create(int transitionId, GuardType guardType, string configuration, int displayOrder)
    {
        return new TransitionGuard
        {
            TransitionId = transitionId,
            GuardType = guardType,
            Configuration = configuration,
            DisplayOrder = displayOrder
        };
    }

    public int TransitionId { get; private set; }
    public GuardType GuardType { get; private set; }
    public string Configuration { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
}
