using MediatR;

namespace ITS.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    Guid LeadUserId,
    string? AvatarUrl,
    Guid? WorkflowId,
    byte[] RowVersion
) : IRequest;
