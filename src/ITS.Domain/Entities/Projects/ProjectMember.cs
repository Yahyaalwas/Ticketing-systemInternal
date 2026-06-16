using ITS.Domain.Common;

namespace ITS.Domain.Entities.Projects;

public class ProjectMember : BaseEntity<Guid>
{
    private ProjectMember() { }

    public static ProjectMember Add(Guid projectId, Guid userId, int roleId, Guid grantedByUserId)
    {
        return new ProjectMember
        {
            Id = Guid.CreateVersion7(),
            ProjectId = projectId,
            UserId = userId,
            RoleId = roleId,
            GrantedAt = DateTime.UtcNow,
            GrantedByUserId = grantedByUserId
        };
    }

    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public int RoleId { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public Guid GrantedByUserId { get; private set; }

    public void ChangeRole(int newRoleId)
        => RoleId = newRoleId;
}
