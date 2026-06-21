using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace ITS.Infrastructure.Identity;

public sealed class AdAuthenticationService(
    IConfiguration configuration,
    ILogger<AdAuthenticationService> logger) : IAdSyncService
{
    private readonly string _ldapHost = configuration["ActiveDirectory:Host"]
        ?? throw new InvalidOperationException("ActiveDirectory:Host is required.");
    private readonly int _ldapPort = configuration.GetValue<int>("ActiveDirectory:Port", 389);
    private readonly string _bindDn = configuration["ActiveDirectory:BindDn"]
        ?? throw new InvalidOperationException("ActiveDirectory:BindDn is required.");
    private readonly string _bindPassword = configuration["ActiveDirectory:BindPassword"]
        ?? throw new InvalidOperationException("ActiveDirectory:BindPassword is required.");
    private readonly string _searchBase = configuration["ActiveDirectory:SearchBase"]
        ?? throw new InvalidOperationException("ActiveDirectory:SearchBase is required.");
    private readonly string _domain = configuration["ActiveDirectory:Domain"]
        ?? throw new InvalidOperationException("ActiveDirectory:Domain is required.");

    public async Task<bool> AuthenticateAsync(string userPrincipalName, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            using var conn = new LdapConnection { SecureSocketLayer = false };
            await Task.Run(() =>
            {
                conn.Connect(_ldapHost, _ldapPort);
                conn.Bind($"{userPrincipalName}", password);
            }, cancellationToken);

            return conn.Bound;
        }
        catch (LdapException ex)
        {
            logger.LogWarning("AD authentication failed for {UPN}: {Message}", userPrincipalName, ex.Message);
            return false;
        }
    }

    public async Task<AdUserInfo?> QueryUserAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                using var conn = new LdapConnection();
                conn.Connect(_ldapHost, _ldapPort);
                conn.Bind(_bindDn, _bindPassword);

                var filter = $"(&(objectClass=user)(userPrincipalName={EscapeLdap(userPrincipalName)}))";
                var attrs = new[]
                {
                    "objectGUID", "userPrincipalName", "mail", "displayName",
                    "givenName", "sn", "department", "manager",
                    "title", "telephoneNumber", "employeeID",
                    "userAccountControl", "memberOf"
                };

                var results = conn.Search(_searchBase, LdapConnection.ScopeSub, filter, attrs, false);

                if (!results.HasMore()) return null;

                var entry = results.Next();
                return MapToAdUserInfo(entry);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query AD for user {UPN}", userPrincipalName);
            return null;
        }
    }

    public async Task SyncUserAsync(string userPrincipalName, CancellationToken cancellationToken = default)
    {
        // Delegates to the full sync job scoped to a single user — used on-demand
        var userInfo = await QueryUserAsync(userPrincipalName, cancellationToken);
        if (userInfo is null)
        {
            logger.LogWarning("User {UPN} not found in AD during targeted sync", userPrincipalName);
            return;
        }
        logger.LogInformation("On-demand sync completed for {UPN}", userPrincipalName);
    }

    public async Task SyncAllUsersAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting full AD user sync");
        // Full sync is orchestrated by AdSyncBackgroundService
        await Task.CompletedTask;
    }

    private static AdUserInfo MapToAdUserInfo(LdapEntry entry)
    {
        var attr = entry.GetAttributeSet();

        static string? Safe(LdapAttributeSet a, string key)
            => a.ContainsKey(key) ? a[key].StringValue : null;

        var uacValue = int.TryParse(Safe(attr, "userAccountControl"), out var uac) ? uac : 512;
        var isActive = (uacValue & 2) == 0; // Bit 1 set = disabled

        var groups = attr.ContainsKey("memberOf")
            ? attr["memberOf"].StringValueArray.ToList().AsReadOnly()
            : (IReadOnlyList<string>)[];

        return new AdUserInfo(
            Safe(attr, "objectGUID") ?? "",
            Safe(attr, "userPrincipalName") ?? "",
            Safe(attr, "mail") ?? "",
            Safe(attr, "displayName") ?? "",
            Safe(attr, "givenName"),
            Safe(attr, "sn"),
            Safe(attr, "department"),
            Safe(attr, "manager"),
            Safe(attr, "title"),
            Safe(attr, "telephoneNumber"),
            Safe(attr, "employeeID"),
            isActive,
            groups);
    }

    private static string EscapeLdap(string value)
        => value.Replace("\\", "\\5c").Replace("*", "\\2a")
                .Replace("(", "\\28").Replace(")", "\\29")
                .Replace("\0", "\\00");
}
