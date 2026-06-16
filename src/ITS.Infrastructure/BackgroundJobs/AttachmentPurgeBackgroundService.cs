using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ITS.Infrastructure.BackgroundJobs;

public sealed class AttachmentPurgeBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AttachmentPurgeBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Attachment purge failed.");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var now = DateTime.UtcNow;
        var due = await db.Attachments
            .IgnoreQueryFilters()
            .Where(a => a.IsDeleted && a.PurgeAfterDate.HasValue && a.PurgeAfterDate.Value <= now)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Purging {Count} soft-deleted attachments", due.Count);

        foreach (var attachment in due)
        {
            try
            {
                await storage.DeleteAsync(attachment.StorageKey, cancellationToken);

                if (attachment.ThumbnailKey is not null)
                    await storage.DeleteAsync(attachment.ThumbnailKey, cancellationToken);

                db.Attachments.Remove(attachment); // Hard delete after purge
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to purge attachment {AttachmentId}", attachment.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Attachment purge complete.");
    }
}
