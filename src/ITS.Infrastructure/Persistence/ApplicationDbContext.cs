using ITS.Application.Common.Interfaces;
using ITS.Domain.Common;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Entities.Config;
using ITS.Domain.Entities.Content;
using ITS.Domain.Entities.Identity;
using ITS.Domain.Entities.Notifications;
using ITS.Domain.Entities.Projects;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Workflow;
using ITS.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly AuditableEntityInterceptor _auditInterceptor;
    private readonly DomainEventDispatchInterceptor _domainEventInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditableEntityInterceptor auditInterceptor,
        DomainEventDispatchInterceptor domainEventInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _domainEventInterceptor = domainEventInterceptor;
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserGlobalRole> UserGlobalRoles => Set<UserGlobalRole>();
    public DbSet<AdGroupRoleMapping> AdGroupRoleMappings => Set<AdGroupRoleMapping>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<IssueType> IssueTypes => Set<IssueType>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldOption> CustomFieldOptions => Set<CustomFieldOption>();

    // Workflow
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowStatus> WorkflowStatuses => Set<WorkflowStatus>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<TransitionGuard> TransitionGuards => Set<TransitionGuard>();
    public DbSet<TransitionAction> TransitionActions => Set<TransitionAction>();

    // Config
    public DbSet<ProjectSequence> ProjectSequences => Set<ProjectSequence>();

    // Tickets
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketLabel> TicketLabels => Set<TicketLabel>();
    public DbSet<TicketLink> TicketLinks => Set<TicketLink>();
    public DbSet<TicketLinkType> TicketLinkTypes => Set<TicketLinkType>();
    public DbSet<TicketWatcher> TicketWatchers => Set<TicketWatcher>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();

    // Content
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentHistory> CommentHistories => Set<CommentHistory>();
    public DbSet<CommentMention> CommentMentions => Set<CommentMention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // Audit
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<EmailQueue> EmailQueue => Set<EmailQueue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditInterceptor, _domainEventInterceptor);
    }
}
