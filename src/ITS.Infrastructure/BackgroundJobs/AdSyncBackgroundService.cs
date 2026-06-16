using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace ITS.Infrastructure.BackgroundJobs;

public sealed class AdSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AdSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AD Sync background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AD sync cycle failed.");
            }

            // Wait for configured interval (default 15 minutes)
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting AD sync at {UtcNow}", DateTime.UtcNow);

        using var scope = scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IAdSyncService>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Fetch all active AD users via LDAP
        await syncService.SyncAllUsersAsync(cancellationToken);

        // Identify users in ITS that are no longer in AD and deactivate them
        var syncedAt = DateTime.UtcNow;
        var staleUsers = await db.Users
            .Where(u => u.IsActive && !u.IsDeleted && u.LastAdSyncAt < syncedAt.AddHours(-1))
            .ToListAsync(cancellationToken);

        foreach (var user in staleUsers)
        {
            var adUser = await syncService.QueryUserAsync(user.UserPrincipalName, cancellationToken);
            if (adUser is null || !adUser.IsActive)
            {
                user.Deactivate();
                logger.LogInformation("Deactivated user {UPN} — no longer active in AD", user.UserPrincipalName);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("AD sync completed. Processed {Count} stale users.", staleUsers.Count);
    }
}
