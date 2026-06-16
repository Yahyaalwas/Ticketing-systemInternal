using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Workflow;

namespace ITS.Infrastructure.Services;

internal sealed class WorkflowEngineService : IWorkflowEngine
{
    public Task<WorkflowValidationResult> ValidateTransitionAsync(
        Ticket ticket, WorkflowTransition transition, Guid actorUserId,
        string? comment, CancellationToken cancellationToken = default)
        => Task.FromResult(WorkflowValidationResult.Success());

    public Task ExecuteActionsAsync(
        Ticket ticket, WorkflowTransition transition, Guid actorUserId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
