using MediatR;

namespace ITS.Application.Features.Projects.Commands.AddMember;

public sealed record AddMemberCommand(
    Guid ProjectId,
    Guid UserId,
    int RoleId
) : IRequest<AddMemberResponse>;

public sealed record AddMemberResponse(Guid MemberId);
