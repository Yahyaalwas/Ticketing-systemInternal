using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ITS.Application.Features.Reference.GetRoles;

public sealed class GetRolesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleRefDto>>
{
    public async Task<IReadOnlyList<RoleRefDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Scope))
            query = query.Where(r => r.Scope == request.Scope);
        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleRefDto(r.Id, r.Name, r.Scope, r.Description))
            .ToListAsync(ct);
    }
}
