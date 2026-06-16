namespace ITS.Application.Common.Interfaces;

public interface IAdSyncService
{
    Task SyncAllUsersAsync(CancellationToken cancellationToken = default);
    Task SyncUserAsync(string userPrincipalName, CancellationToken cancellationToken = default);
    Task<AdUserInfo?> QueryUserAsync(string userPrincipalName, CancellationToken cancellationToken = default);
    Task<bool> AuthenticateAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default);
}

public record AdUserInfo(
    string ObjectId,
    string UserPrincipalName,
    string Email,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Department,
    string? Manager,
    string? JobTitle,
    string? PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> GroupMemberships);
