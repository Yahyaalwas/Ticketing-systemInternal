using ITS.Domain.Common;

namespace ITS.Domain.Entities.Identity;

public class AdGroupRoleMapping : BaseEntity<int>
{
    private AdGroupRoleMapping() { }

    public static AdGroupRoleMapping Create(
        string adGroupDistinguishedName,
        string adGroupName,
        int roleId,
        Guid? projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adGroupDistinguishedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(adGroupName);

        return new AdGroupRoleMapping
        {
            AdGroupDistinguishedName = adGroupDistinguishedName,
            AdGroupName = adGroupName,
            RoleId = roleId,
            ProjectId = projectId
        };
    }

    public string AdGroupDistinguishedName { get; private set; } = default!;
    public string AdGroupName { get; private set; } = default!;
    public int RoleId { get; private set; }
    public Guid? ProjectId { get; private set; }

    // Navigation
    public Role Role { get; private set; } = default!;
}
