using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserDto?>
{
    public async Task<CurrentUserDto?> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.GlobalRoles).ThenInclude(gr => gr.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId && u.IsActive, cancellationToken);

        if (user is null) return null;

        return new CurrentUserDto(
            user.Id,
            user.UserPrincipalName,
            user.DisplayName,
            user.Email,
            user.EmployeeId,
            user.AvatarUrl,
            user.TimeZoneId,
            user.Locale,
            user.GlobalRoles.Select(gr => gr.Role.Name).ToList().AsReadOnly());
    }
}
