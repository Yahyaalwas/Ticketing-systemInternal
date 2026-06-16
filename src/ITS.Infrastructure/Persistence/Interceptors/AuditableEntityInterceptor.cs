using ITS.Application.Common.Interfaces;
using ITS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ITS.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(
    ICurrentUserService currentUserService,
    IDateTimeService dateTimeService) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        var now = dateTimeService.UtcNow;
        var userId = currentUserService.IsAuthenticated
            ? currentUserService.UserId
            : SystemUser.Id; // Sentinel for background jobs

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity<Guid>>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.SetAuditFieldsOnCreate(userId, now);

            if (entry.State is EntityState.Modified or EntityState.Added)
                entry.Entity.SetAuditFieldsOnUpdate(userId, now);
        }

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity<int>>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.SetAuditFieldsOnCreate(userId, now);

            if (entry.State is EntityState.Modified or EntityState.Added)
                entry.Entity.SetAuditFieldsOnUpdate(userId, now);
        }
    }
}

// Sentinel user ID for system/background operations
public static class SystemUser
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000000001");
}
