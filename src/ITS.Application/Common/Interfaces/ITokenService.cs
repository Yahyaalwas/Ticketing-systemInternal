namespace ITS.Application.Common.Interfaces;

public interface ITokenService
{
    string IssueToken(TokenRequest request);
}

public sealed record TokenRequest(
    Guid UserId,
    string UserPrincipalName,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles);
