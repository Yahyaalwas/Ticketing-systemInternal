using ITS.Domain.Common;

namespace ITS.Domain.Entities.Identity;

public class Department : AuditableEntity<int>
{
    private Department() { }

    public static Department Create(string name, string? description, int? parentDepartmentId, string? adOuDistinguishedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Department
        {
            Name = name,
            Description = description,
            ParentDepartmentId = parentDepartmentId,
            AdOuDistinguishedName = adOuDistinguishedName
        };
    }

    public string? AdOuDistinguishedName { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? ParentDepartmentId { get; private set; }
    public Guid? HeadUserId { get; private set; }

    // Navigation properties
    public Department? ParentDepartment { get; private set; }
    public ICollection<Department> ChildDepartments { get; private set; } = [];
    public ICollection<User> Users { get; private set; } = [];

    public void Update(string name, string? description, Guid? headUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        HeadUserId = headUserId;
    }

    public void SyncFromAd(string name, string adOuDistinguishedName)
    {
        Name = name;
        AdOuDistinguishedName = adOuDistinguishedName;
    }
}
