using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ITS.Application.Features.Reference.SearchUsers;

public sealed class SearchUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<SearchUsersQuery, IReadOnlyList<UserRefDto>>
{
    public async Task<IReadOnlyList<UserRefDto>> Handle(SearchUsersQuery request, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search;
            query = query.Where(u => u.DisplayName.Contains(s) || u.Email.Contains(s));
        }
        return await query
            .OrderBy(u => u.DisplayName)
            .Take(request.Limit)
            .Select(u => new UserRefDto(u.Id, u.DisplayName, u.Email, u.AvatarUrl, u.IsActive))
            .ToListAsync(ct);
    }
}
