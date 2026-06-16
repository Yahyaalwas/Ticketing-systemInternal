using ITS.Domain.Entities.Tickets;
using ITS.Domain.Entities.Workflow;

namespace ITS.Application.Common.Interfaces;

public interface IWorkflowEngine
{
    Task<WorkflowValidationResult> ValidateTransitionAsync(
        Ticket ticket,
        WorkflowTransition transition,
        Guid actorUserId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task ExecuteActionsAsync(
        Ticket ticket,
        WorkflowTransition transition,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public record WorkflowValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static WorkflowValidationResult Success() => new(true, []);
    public static WorkflowValidationResult Failure(params string[] errors) => new(false, errors);
}
