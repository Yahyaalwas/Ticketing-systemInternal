using ITS.Domain.Entities.Audit;
using ITS.Domain.Entities.Content;
using ITS.Domain.Entities.Identity;
using ITS.Domain.Entities.Notifications;
using ITS.Domain.Entities.Projects;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Workflow;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Identity
    DbSet<User> Users { get; }
    DbSet<Department> Departments { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserGlobalRole> UserGlobalRoles { get; }
    DbSet<AdGroupRoleMapping> AdGroupRoleMappings { get; }

    // Projects
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }
    DbSet<IssueType> IssueTypes { get; }
    DbSet<Priority> Priorities { get; }
    DbSet<Label> Labels { get; }
    DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; }
    DbSet<CustomFieldOption> CustomFieldOptions { get; }

    // Workflow
    DbSet<Workflow> Workflows { get; }
    DbSet<WorkflowStatus> WorkflowStatuses { get; }
    DbSet<WorkflowTransition> WorkflowTransitions { get; }
    DbSet<TransitionGuard> TransitionGuards { get; }
    DbSet<TransitionAction> TransitionActions { get; }

    // Tickets
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketLabel> TicketLabels { get; }
    DbSet<TicketLink> TicketLinks { get; }
    DbSet<TicketLinkType> TicketLinkTypes { get; }
    DbSet<TicketWatcher> TicketWatchers { get; }
    DbSet<CustomFieldValue> CustomFieldValues { get; }
    DbSet<Resolution> Resolutions { get; }

    // Content
    DbSet<Comment> Comments { get; }
    DbSet<CommentHistory> CommentHistories { get; }
    DbSet<CommentMention> CommentMentions { get; }
    DbSet<Attachment> Attachments { get; }

    // Audit
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // Notifications
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<EmailQueue> EmailQueue { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
