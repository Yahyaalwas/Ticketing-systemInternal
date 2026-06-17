using MediatR;

namespace ITS.Application.Features.Projects.Commands.RemoveMember;

public sealed record RemoveMemberCommand(Guid ProjectId, Guid UserId) : IRequest;
