using ITS.Domain.Common;
using ITS.Domain.Enums;

namespace ITS.Domain.Entities.Identity;

public class Role : BaseEntity<int>
{
    private Role() { }

    public static Role Create(string name, string? description, RoleScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Role
        {
            Name = name,
            Description = description,
            Scope = scope,
            IsSystemRole = false
        };
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public RoleScope Scope { get; private set; }
    public bool IsSystemRole { get; private set; }
}
