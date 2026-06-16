using MediatR;

namespace ITS.Application.Features.Auth.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto?>;

public sealed record CurrentUserDto(
    Guid UserId,
    string Upn,
    string DisplayName,
    string Email,
    string? EmployeeId,
    string? AvatarUrl,
    string TimeZoneId,
    string Locale,
    IReadOnlyList<string> Roles);
