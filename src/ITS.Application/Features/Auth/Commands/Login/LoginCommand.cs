using MediatR;

namespace ITS.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string UserPrincipalName,
    string Password) : IRequest<LoginResult>;

public sealed record LoginResult(
    string Token,
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    IReadOnlyList<string> Roles);
