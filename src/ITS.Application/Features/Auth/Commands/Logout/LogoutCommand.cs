using MediatR;

namespace ITS.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(
    Guid UserId,
    string? IpAddress,
    string? UserAgent,
    string TraceId) : IRequest;
