using ITS.Domain.Entities.Identity;
using ITS.Domain.Entities.Projects;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITS.Infrastructure.Persistence.Configurations;

/// <summary>
/// Seeds static reference data via HasData so it is included in the InitialCreate migration.
/// All entities here are immutable lookup tables with deterministic integer IDs.
/// </summary>
public class RoleSeedConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasData(
            CreateRole(1, "System Administrator", "Full system access.",                        RoleScope.Global, isSystem: true),
            CreateRole(2, "Department Manager",   "Manages a department and its projects.",    RoleScope.Global, isSystem: true),
            CreateRole(3, "Project Lead",         "Manages projects and project members.",     RoleScope.Global, isSystem: false),
            CreateRole(4, "Member",               "Active project contributor.",               RoleScope.Global, isSystem: false),
            CreateRole(5, "Developer",            "Creates and resolves tickets.",             RoleScope.Global, isSystem: false),
            CreateRole(6, "Reporter",             "Creates tickets and reports issues.",       RoleScope.Global, isSystem: false),
            CreateRole(7, "Viewer",               "Read-only access to projects and tickets.", RoleScope.Global, isSystem: true)
        );
    }

    private static object CreateRole(int id, string name, string description, RoleScope scope, bool isSystem)
        => new { Id = id, Name = name, Description = description, Scope = scope, IsSystemRole = isSystem };
}

public class PrioritySeedConfiguration : IEntityTypeConfiguration<Priority>
{
    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.HasData(
            new { Id = 1, Name = "Low",      Description = "Low priority — address when convenient.",         Color = "#6c757d", DisplayOrder = 1, SlaTargetHours = (int?)72, IsActive = true, IsSystemPriority = true, IconUrl = (string?)null },
            new { Id = 2, Name = "Medium",   Description = "Medium priority — address within this sprint.",   Color = "#0d6efd", DisplayOrder = 2, SlaTargetHours = (int?)24, IsActive = true, IsSystemPriority = true, IconUrl = (string?)null },
            new { Id = 3, Name = "High",     Description = "High priority — address today.",                  Color = "#fd7e14", DisplayOrder = 3, SlaTargetHours = (int?)8,  IsActive = true, IsSystemPriority = true, IconUrl = (string?)null },
            new { Id = 4, Name = "Critical", Description = "Critical — production impacting, fix immediately.",Color = "#dc3545", DisplayOrder = 4, SlaTargetHours = (int?)2,  IsActive = true, IsSystemPriority = true, IconUrl = (string?)null }
        );
    }
}

public class ResolutionSeedConfiguration : IEntityTypeConfiguration<Resolution>
{
    public void Configure(EntityTypeBuilder<Resolution> builder)
    {
        builder.HasData(
            new { Id = 1, Name = "Fixed",           Description = "The issue has been fixed.",                   DisplayOrder = 1, IsActive = true, IsSystemDefault = true  },
            new { Id = 2, Name = "Won't Fix",       Description = "The team has decided not to fix this issue.", DisplayOrder = 2, IsActive = true, IsSystemDefault = false },
            new { Id = 3, Name = "Duplicate",       Description = "This issue is a duplicate of another.",       DisplayOrder = 3, IsActive = true, IsSystemDefault = false },
            new { Id = 4, Name = "Cannot Reproduce", Description = "The issue could not be reproduced.",        DisplayOrder = 4, IsActive = true, IsSystemDefault = false },
            new { Id = 5, Name = "By Design",       Description = "The behaviour is by design.",                 DisplayOrder = 5, IsActive = true, IsSystemDefault = false }
        );
    }
}

public class TicketLinkTypeSeedConfiguration : IEntityTypeConfiguration<TicketLinkType>
{
    public void Configure(EntityTypeBuilder<TicketLinkType> builder)
    {
        builder.HasData(
            new { Id = 1, Name = "Blocks",     InwardName = "is blocked by", OutwardName = "blocks"      },
            new { Id = 2, Name = "Relates To", InwardName = "relates to",    OutwardName = "relates to"  },
            new { Id = 3, Name = "Duplicates", InwardName = "is duplicated by", OutwardName = "duplicates" },
            new { Id = 4, Name = "Clones",     InwardName = "is cloned by",  OutwardName = "clones"      }
        );
    }
}
