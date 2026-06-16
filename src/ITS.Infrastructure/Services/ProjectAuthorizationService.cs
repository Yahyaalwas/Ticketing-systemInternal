using ITS.Application.Common.Interfaces;

namespace ITS.Infrastructure.Services;

internal sealed class ProjectAuthorizationService : IProjectAuthorizationService
{
    public Task<bool> CanViewProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> CanManageProjectAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> CanCreateTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> CanEditTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> CanDeleteTicketAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> CanTransitionTicketAsync(Guid userId, Guid projectId, int requiredRoleId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<Guid>> GetAccessibleProjectIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Guid>>([]);
}
