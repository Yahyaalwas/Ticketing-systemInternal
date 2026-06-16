namespace ITS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserPrincipalName { get; }
    string DisplayName { get; }
    IReadOnlyList<string> Roles { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
