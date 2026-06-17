using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ITS.Application.Features.Reference.GetPriorities;

public sealed class GetPrioritiesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPrioritiesQuery, IReadOnlyList<PriorityDto>>
{
    public async Task<IReadOnlyList<PriorityDto>> Handle(GetPrioritiesQuery request, CancellationToken ct)
        => await db.Priorities
            .AsNoTracking()
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PriorityDto(p.Id, p.Name, p.Color, p.SlaTargetHours, p.DisplayOrder))
            .ToListAsync(ct);
}
