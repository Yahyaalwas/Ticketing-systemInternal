using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.Identity;

/// <summary>
/// Development-only substitute for Active Directory.
/// Authenticates against users already in the local database (seeded by
/// ApplicationDbContextSeed / seed-demo-data.sql). Any password is accepted.
///
/// NEVER register this service in Production.
/// </summary>
public sealed class DevAdSyncService(
    ApplicationDbContext db,
    ILogger<DevAdSyncService> logger) : IAdSyncService
{
    // Pre-defined dev accounts that are seeded on first startup.
    // Key = UPN (lower-cased), Value = display name
    private static readonly Dictionary<string, (string DisplayName, string First, string Last, string Role)> DevUsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@demo.its"]  = ("Alice Admin",      "Alice", "Admin",     "System Administrator"),
        ["lead@demo.its"]   = ("Bob Lead",          "Bob",   "Lead",      "Project Lead"),
        ["dev1@demo.its"]   = ("Carol Developer",   "Carol", "Developer", "Member"),
        ["dev2@demo.its"]   = ("Dave Developer",    "Dave",  "Developer", "Member"),
    };

    public Task<bool> AuthenticateAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default)
    {
        var ok = DevUsers.ContainsKey(userPrincipalName);
        if (!ok)
            logger.LogWarning("[DEV] Unknown dev user: {UPN}", userPrincipalName);
        else
            logger.LogInformation("[DEV] Auth accepted for {UPN} (dev mode — any password)", userPrincipalName);
        return Task.FromResult(ok);
    }

    public Task<AdUserInfo?> QueryUserAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        if (!DevUsers.TryGetValue(userPrincipalName, out var info))
            return Task.FromResult<AdUserInfo?>(null);

        var adUser = new AdUserInfo(
            ObjectId: $"dev-{userPrincipalName}",
            UserPrincipalName: userPrincipalName,
            Email: userPrincipalName,
            DisplayName: info.DisplayName,
            FirstName: info.First,
            LastName: info.Last,
            Department: "Engineering",
            Manager: null,
            JobTitle: info.Last == "Admin" ? "System Administrator" : "Developer",
            PhoneNumber: null,
            EmployeeId: null,
            IsActive: true,
            GroupMemberships: []);

        return Task.FromResult<AdUserInfo?>(adUser);
    }

    public Task SyncAllUsersAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[DEV] SyncAllUsers is a no-op in dev mode.");
        return Task.CompletedTask;
    }

    public Task SyncUserAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[DEV] SyncUser is a no-op in dev mode.");
        return Task.CompletedTask;
    }
}
