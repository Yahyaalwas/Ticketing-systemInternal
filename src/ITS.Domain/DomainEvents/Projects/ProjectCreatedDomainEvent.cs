using ITS.Domain.Common;

namespace ITS.Domain.DomainEvents.Projects;

public sealed record ProjectCreatedDomainEvent(
    Guid ProjectId,
    string ProjectKey,
    string ProjectName) : DomainEvent;
