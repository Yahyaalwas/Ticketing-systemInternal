namespace ITS.Domain.Exceptions;

public class WorkflowTransitionException : DomainException
{
    public int FromStatusId { get; }
    public int ToStatusId { get; }

    public WorkflowTransitionException(int fromStatusId, int toStatusId, string reason)
        : base($"Transition from status {fromStatusId} to {toStatusId} is not allowed: {reason}")
    {
        FromStatusId = fromStatusId;
        ToStatusId = toStatusId;
    }
}
