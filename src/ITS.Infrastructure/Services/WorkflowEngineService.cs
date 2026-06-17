using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Workflow;
using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ITS.Infrastructure.Services;

internal sealed class WorkflowEngineService(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ILogger<WorkflowEngineService> logger) : IWorkflowEngine
{
    public async Task<WorkflowValidationResult> ValidateTransitionAsync(
        Ticket ticket,
        WorkflowTransition transition,
        Guid actorUserId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        foreach (var guard in transition.Guards.OrderBy(g => g.DisplayOrder))
        {
            var error = await EvaluateGuardAsync(ticket, guard, actorUserId, comment, cancellationToken);
            if (error is not null)
                errors.Add(error);
        }

        return errors.Count == 0
            ? WorkflowValidationResult.Success()
            : WorkflowValidationResult.Failure([.. errors]);
    }

    public async Task ExecuteActionsAsync(
        Ticket ticket,
        WorkflowTransition transition,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        foreach (var action in transition.Actions.OrderBy(a => a.DisplayOrder))
        {
            try
            {
                await ExecuteActionAsync(ticket, action, actorUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Transition action {ActionType} failed on ticket {TicketId}. Continuing.",
                    action.ActionType, ticket.Id);
            }
        }
    }

    private async Task<string?> EvaluateGuardAsync(
        Ticket ticket,
        TransitionGuard guard,
        Guid actorUserId,
        string? comment,
        CancellationToken cancellationToken)
    {
        return guard.GuardType switch
        {
            GuardType.RoleCheck => await EvaluateRoleCheckAsync(actorUserId, ticket.ProjectId, guard.Configuration, cancellationToken),
            GuardType.FieldRequired => EvaluateFieldRequired(ticket, guard.Configuration),
            GuardType.ApprovalRequired => await EvaluateApprovalRequiredAsync(ticket, guard.Configuration, cancellationToken),
            GuardType.WebhookCall => await EvaluateWebhookGuardAsync(ticket, guard.Configuration, cancellationToken),
            _ => null
        };
    }

    private async Task<string?> EvaluateRoleCheckAsync(Guid actorUserId, Guid projectId, string configuration, CancellationToken cancellationToken)
    {
        var config = ParseConfig(configuration);
        if (!config.TryGetProperty("roleId", out var roleIdElement))
            return null;

        var roleId = roleIdElement.GetInt32();
        var isMember = await db.ProjectMembers
            .AsNoTracking()
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == actorUserId && pm.RoleId == roleId,
                cancellationToken);

        if (!isMember)
        {
            var roleName = await db.Roles.AsNoTracking()
                .Where(r => r.Id == roleId)
                .Select(r => r.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? $"role {roleId}";

            return $"You must have the '{roleName}' role to perform this transition.";
        }

        return null;
    }

    private static string? EvaluateFieldRequired(Ticket ticket, string configuration)
    {
        var config = ParseConfig(configuration);
        if (!config.TryGetProperty("fieldName", out var fieldNameElement))
            return null;

        var fieldName = fieldNameElement.GetString() ?? string.Empty;

        var missing = fieldName.ToLowerInvariant() switch
        {
            "title" => string.IsNullOrWhiteSpace(ticket.Title),
            "description" => string.IsNullOrWhiteSpace(ticket.Description),
            "assignee" => !ticket.AssigneeUserId.HasValue,
            "duedate" => !ticket.DueDate.HasValue,
            "storypoints" => !ticket.StoryPoints.HasValue,
            _ => false
        };

        return missing ? $"Field '{fieldName}' is required to perform this transition." : null;
    }

    private async Task<string?> EvaluateApprovalRequiredAsync(Ticket ticket, string configuration, CancellationToken cancellationToken)
    {
        var config = ParseConfig(configuration);
        var approvalFieldId = config.TryGetProperty("customFieldId", out var idEl) ? idEl.GetInt32() : 0;

        if (approvalFieldId > 0)
        {
            var fieldValue = await db.CustomFieldValues
                .AsNoTracking()
                .Where(cfv => cfv.TicketId == ticket.Id && cfv.CustomFieldId == approvalFieldId)
                .Select(cfv => cfv.ValueText)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.Equals(fieldValue, "Approved", StringComparison.OrdinalIgnoreCase))
                return "This ticket requires approval before it can be transitioned.";
        }

        return null;
    }

    private async Task<string?> EvaluateWebhookGuardAsync(Ticket ticket, string configuration, CancellationToken cancellationToken)
    {
        // Webhook guards are asynchronous calls to external systems.
        // In this implementation we log and allow — integrate an IHttpClientFactory here when needed.
        logger.LogInformation(
            "WebhookCall guard evaluated (pass-through) for ticket {TicketId} with config {Config}",
            ticket.Id, configuration);

        await Task.CompletedTask;
        return null;
    }

    private async Task ExecuteActionAsync(Ticket ticket, TransitionAction action, Guid actorUserId, CancellationToken cancellationToken)
    {
        switch (action.ActionType)
        {
            case TransitionActionType.SetField:
                ExecuteSetField(ticket, action.Configuration);
                break;

            case TransitionActionType.SendNotification:
                await ExecuteSendNotificationAsync(ticket, action.Configuration, actorUserId, cancellationToken);
                break;

            case TransitionActionType.CreateComment:
                ExecuteCreateComment(ticket, action.Configuration, actorUserId);
                break;

            case TransitionActionType.WebhookCall:
                logger.LogInformation(
                    "WebhookCall action skipped for ticket {TicketId} — integrate IHttpClientFactory when needed.",
                    ticket.Id);
                break;
        }
    }

    private static void ExecuteSetField(Ticket ticket, string configuration)
    {
        var config = ParseConfig(configuration);
        if (!config.TryGetProperty("fieldName", out var fieldNameEl))
            return;

        var fieldName = fieldNameEl.GetString() ?? string.Empty;
        var value = config.TryGetProperty("value", out var valueEl) ? valueEl.GetString() : null;

        switch (fieldName.ToLowerInvariant())
        {
            case "priority":
                if (int.TryParse(value, out var priorityId))
                    ticket.ChangePriority(priorityId);
                break;

            case "duedate":
                if (DateOnly.TryParse(value, out var date))
                    ticket.SetDueDate(date);
                break;
        }
    }

    private async Task ExecuteSendNotificationAsync(Ticket ticket, string configuration, Guid actorUserId, CancellationToken cancellationToken)
    {
        var config = ParseConfig(configuration);
        var notificationType = config.TryGetProperty("type", out var typeEl)
            ? Enum.TryParse<NotificationType>(typeEl.GetString(), out var nt) ? nt : NotificationType.WatchedTicketUpdated
            : NotificationType.WatchedTicketUpdated;

        var title = config.TryGetProperty("title", out var titleEl)
            ? titleEl.GetString() ?? "Ticket status changed"
            : "Ticket status changed";

        // Notify all watchers
        var watcherIds = ticket.Watchers.Select(w => w.UserId).ToList();
        if (watcherIds.Count > 0)
        {
            await notificationService.NotifyManyAsync(
                watcherIds, notificationType, title, null,
                ticket.Id, ticket.ProjectId, actorUserId, cancellationToken);
        }
    }

    private void ExecuteCreateComment(Ticket ticket, string configuration, Guid actorUserId)
    {
        var config = ParseConfig(configuration);
        var body = config.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(body))
            return;

        var comment = Domain.Entities.Content.Comment.Create(ticket.Id, null, actorUserId, body, null);
        db.Comments.Add(comment);
    }

    private static JsonElement ParseConfig(string configuration)
    {
        try
        {
            return JsonDocument.Parse(configuration).RootElement;
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement;
        }
    }
}
