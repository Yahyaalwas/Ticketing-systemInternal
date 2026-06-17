using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Ai;
using ITS.Infrastructure.Ai.Providers;
using ITS.Infrastructure.BackgroundJobs;
using ITS.Infrastructure.Identity;
using ITS.Infrastructure.Persistence;
using ITS.Infrastructure.Persistence.Interceptors;
using ITS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        services.AddScoped<IWorkflowEngine, WorkflowEngineService>();
        services.AddScoped<IProjectAuthorizationService, ProjectAuthorizationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();

        // AD / Identity
        services.AddScoped<IAdSyncService, AdAuthenticationService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // Background services
        services.AddHostedService<AdSyncBackgroundService>();
        services.AddHostedService<EmailSenderBackgroundService>();
        services.AddHostedService<AttachmentPurgeBackgroundService>();

        // Seed data
        services.AddScoped<ApplicationDbContextSeed>();

        // Health checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("sql-server");

        // AI Platform
        services.AddMemoryCache();
        services.AddSingleton<IAiCacheService, AiCacheService>();
        services.AddSingleton<IAiAuditService, AiAuditService>();
        services.AddSingleton<IAiRateLimiter, AiRateLimiter>();
        services.AddSingleton<IAiDataMasker, AiDataMasker>();

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<AiRateLimiterOptions>(configuration.GetSection(AiRateLimiterOptions.SectionName));
        services.Configure<AiDataMaskerOptions>(configuration.GetSection(AiDataMaskerOptions.SectionName));
        services.Configure<OpenAiProviderOptions>(configuration.GetSection(OpenAiProviderOptions.SectionName));
        services.Configure<AzureOpenAiProviderOptions>(configuration.GetSection(AzureOpenAiProviderOptions.SectionName));

        // Register the active AI provider based on configuration
        var aiProvider = configuration["Ai:Provider"] ?? "mock";
        switch (aiProvider.ToLowerInvariant())
        {
            case "openai":
                services.AddHttpClient("openai", (sp, c) =>
                {
                    var key = configuration["Ai:OpenAi:ApiKey"] ?? "";
                    c.BaseAddress = new Uri(configuration["Ai:OpenAi:BaseUrl"] ?? "https://api.openai.com");
                    c.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);
                    c.Timeout = TimeSpan.FromSeconds(60);
                });
                services.AddSingleton<IAiProvider, OpenAiProvider>();
                break;

            case "azure-openai":
                services.AddHttpClient("azure-openai", (sp, c) =>
                {
                    var key = configuration["Ai:AzureOpenAi:ApiKey"] ?? "";
                    c.DefaultRequestHeaders.Add("api-key", key);
                    c.Timeout = TimeSpan.FromSeconds(60);
                });
                services.AddSingleton<IAiProvider, AzureOpenAiProvider>();
                break;

            default:
                services.AddSingleton<IAiProvider, MockAiProvider>();
                break;
        }

        return services;
    }
}
