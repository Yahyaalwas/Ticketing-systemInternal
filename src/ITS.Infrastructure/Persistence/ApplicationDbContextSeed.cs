using ITS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Persistence;

/// <summary>
/// Seeds reference data that cannot be expressed as HasData (requires navigation properties
/// or complex factory methods). Runs only when data is absent — idempotent on repeat calls.
/// </summary>
public sealed class ApplicationDbContextSeed(
    ApplicationDbContext db,
    ILogger<ApplicationDbContextSeed> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedDefaultWorkflowAsync(cancellationToken);
        logger.LogInformation("Seed data verified.");
    }

    private async Task SeedDefaultWorkflowAsync(CancellationToken cancellationToken)
    {
        const string workflowName = "Default Software Workflow";

        if (await db.Workflows.AnyAsync(w => w.Name == workflowName, cancellationToken))
            return;

        // Create template workflow (null ProjectId = global template)
        var workflow = Domain.Entities.Workflow.Workflow.Create(
            workflowName,
            "Standard software development lifecycle workflow.",
            isTemplate: true,
            projectId: null);

        db.Workflows.Add(workflow);

        // Statuses must be persisted before transitions can reference their IDs.
        // We save here so EF assigns the int identity PKs.
        await db.SaveChangesAsync(cancellationToken);

        var newStatus      = workflow.AddStatus("New",         null, StatusCategory.ToDo,       "#6c757d", 1, isInitial: true,  isFinal: false);
        var openStatus     = workflow.AddStatus("Open",        null, StatusCategory.ToDo,       "#0d6efd", 2, isInitial: false, isFinal: false);
        var inProgressStatus = workflow.AddStatus("In Progress", null, StatusCategory.InProgress, "#fd7e14", 3, isInitial: false, isFinal: false);
        var reviewStatus   = workflow.AddStatus("Review",      null, StatusCategory.InProgress, "#6f42c1", 4, isInitial: false, isFinal: false);
        var doneStatus     = workflow.AddStatus("Done",        null, StatusCategory.Done,     "#198754", 5, isInitial: false, isFinal: true);
        var closedStatus   = workflow.AddStatus("Closed",      null, StatusCategory.Done,     "#495057", 6, isInitial: false, isFinal: true);

        await db.SaveChangesAsync(cancellationToken);

        // Forward transitions
        workflow.AddTransition("Start",        newStatus.Id,        openStatus.Id,       requiresComment: false, displayOrder: 1);
        workflow.AddTransition("Begin Work",   openStatus.Id,       inProgressStatus.Id, requiresComment: false, displayOrder: 2);
        workflow.AddTransition("Submit Review",inProgressStatus.Id, reviewStatus.Id,     requiresComment: false, displayOrder: 3);
        workflow.AddTransition("Approve",      reviewStatus.Id,     doneStatus.Id,       requiresComment: false, displayOrder: 4);
        workflow.AddTransition("Close",        doneStatus.Id,       closedStatus.Id,     requiresComment: false, displayOrder: 5);

        // Back / loop transitions
        workflow.AddTransition("Request Changes", reviewStatus.Id, inProgressStatus.Id, requiresComment: true,  displayOrder: 6);
        workflow.AddTransition("Reopen",          closedStatus.Id, openStatus.Id,       requiresComment: true,  displayOrder: 7);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded default workflow '{WorkflowName}' with {StatusCount} statuses and {TransitionCount} transitions.",
            workflowName, 6, 7);
    }
}
