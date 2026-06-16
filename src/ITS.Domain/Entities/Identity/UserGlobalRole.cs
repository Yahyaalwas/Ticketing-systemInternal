using ITS.Domain.Common;

namespace ITS.Domain.Entities.Identity;

public class UserGlobalRole : BaseEntity<int>
{
    private UserGlobalRole() { }

    public static UserGlobalRole Grant(Guid userId, int roleId, Guid grantedByUserId)
    {
        return new UserGlobalRole
        {
            UserId = userId,
            RoleId = roleId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        };
    }

    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public Guid GrantedByUserId { get; private set; }

    // Navigation
    public User User { get; private set; } = default!;
    public Role Role { get; private set; } = default!;
}
