using ITS.Domain.Common;

namespace ITS.Domain.Entities.Tickets;

public class Resolution : BaseEntity<int>
{
    private Resolution() { }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystemDefault { get; private set; }
}
