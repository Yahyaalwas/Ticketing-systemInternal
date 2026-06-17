using MediatR;

namespace ITS.Application.Features.Projects.Commands.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ProjectId) : IRequest;
