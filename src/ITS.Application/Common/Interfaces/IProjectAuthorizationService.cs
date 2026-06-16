namespace ITS.Application.Common.Interfaces;

public interface IProjectAuthorizationService
{
    Task<bool> CanViewProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanManageProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanCreateTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanEditTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanDeleteTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanTransitionTicketAsync(Guid userId, Guid projectId, int requiredRoleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAccessibleProjectIdsAsync(Guid userId, CancellationToken cancellationToken = default);
}
