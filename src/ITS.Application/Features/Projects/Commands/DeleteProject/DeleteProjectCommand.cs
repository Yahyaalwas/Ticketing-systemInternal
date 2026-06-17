using MediatR;

namespace ITS.Application.Features.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(Guid ProjectId) : IRequest;
