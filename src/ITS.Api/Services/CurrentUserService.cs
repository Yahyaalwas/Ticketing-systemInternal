using ITS.Application.Common.Interfaces;
using System.Security.Claims;

namespace ITS.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    public string UserPrincipalName
        => User?.FindFirstValue(ClaimTypes.Upn)
           ?? User?.FindFirstValue("upn")
           ?? string.Empty;

    public string DisplayName
        => User?.FindFirstValue(ClaimTypes.Name)
           ?? User?.FindFirstValue("name")
           ?? string.Empty;

    public IReadOnlyList<string> Roles
        => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
           ?? [];

    public string? IpAddress
        => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
        => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
