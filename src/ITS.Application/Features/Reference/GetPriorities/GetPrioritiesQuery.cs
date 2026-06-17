using MediatR;
namespace ITS.Application.Features.Reference.GetPriorities;

public sealed record GetPrioritiesQuery : IRequest<IReadOnlyList<PriorityDto>>;
public sealed record PriorityDto(int Id, string Name, string? Color, int? SlaTargetHours, int DisplayOrder);
