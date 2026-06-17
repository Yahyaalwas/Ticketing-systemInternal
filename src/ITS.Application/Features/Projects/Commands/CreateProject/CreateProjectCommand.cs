using MediatR;

namespace ITS.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    string ProjectKey,
    string Name,
    string? Description,
    Guid LeadUserId,
    int DepartmentId,
    Guid? WorkflowId
) : IRequest<CreateProjectResponse>;

public sealed record CreateProjectResponse(Guid ProjectId, string ProjectKey, string Name);
