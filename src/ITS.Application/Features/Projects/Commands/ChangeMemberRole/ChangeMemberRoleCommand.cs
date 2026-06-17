using MediatR;

namespace ITS.Application.Features.Projects.Commands.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(Guid ProjectId, Guid UserId, int NewRoleId) : IRequest;
