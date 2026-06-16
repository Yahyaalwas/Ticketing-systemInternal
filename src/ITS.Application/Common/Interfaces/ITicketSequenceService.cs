namespace ITS.Application.Common.Interfaces;

public interface ITicketSequenceService
{
    Task<int> NextNumberAsync(Guid projectId, CancellationToken cancellationToken = default);
}
