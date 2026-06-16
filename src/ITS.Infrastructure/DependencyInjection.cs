using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.BackgroundJobs;
using ITS.Infrastructure.Identity;
using ITS.Infrastructure.Persistence;
using ITS.Infrastructure.Persistence.Interceptors;
using ITS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // EF Interceptors
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        // Application services
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IMarkdownService, MarkdownService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ITicketSequenceService, TicketSequenceService>();

        // AD / Identity
        services.AddScoped<IAdSyncService, AdAuthenticationService>();

        // Background services
        services.AddHostedService<AdSyncBackgroundService>();
        services.AddHostedService<EmailSenderBackgroundService>();
        services.AddHostedService<AttachmentPurgeBackgroundService>();

        return services;
    }
}
