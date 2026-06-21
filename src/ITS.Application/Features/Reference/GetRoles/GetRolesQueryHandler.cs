using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ITS.Application.Features.Reference.GetRoles;

public sealed class GetRolesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleRefDto>>
{
    public async Task<IReadOnlyList<RoleRefDto>> Handle(GetRolesQuery request, CancellationToken ct)
    {
        var query = db.Roles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Scope) &&
            Enum.TryParse<RoleScope>(request.Scope, ignoreCase: true, out var scopeEnum))
            query = query.Where(r => r.Scope == scopeEnum);
        return await query
            .OrderBy(r => r.Name)
            .Select(r => new RoleRefDto(r.Id, r.Name, r.Scope.ToString(), r.Description))
            .ToListAsync(ct);
    }
}
