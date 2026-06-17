using MediatR;

namespace ITS.Application.Features.Projects.Commands.AssignProjectLead;

public sealed record AssignProjectLeadCommand(Guid ProjectId, Guid NewLeadUserId) : IRequest;
