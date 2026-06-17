using ITS.Api.Extensions;
using ITS.Application;
using ITS.Infrastructure;
using ITS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using System.Text.Json;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ITS API...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/its-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
    });

    // Layers
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    var app = builder.Build();

    // Auto-migrate in development (use explicit migrations in production)
    // Auto-migrate in Development; use explicit migration scripts in Staging/Production.
    if (app.Environment.IsDevelopment())
    {
        using var migrationScope = app.Services.CreateScope();
        var db = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    // Seed reference data — idempotent, safe to run in every environment.
    using (var seedScope = app.Services.CreateScope())
    {
        var seeder = seedScope.ServiceProvider.GetRequiredService<ApplicationDbContextSeed>();
        await seeder.SeedAsync();
    }

    // Middleware pipeline (order matters)
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ITS API v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
        });
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId is not null) diagnosticContext.Set("UserId", userId);
        };
    });

    app.UseCors("ITSCorsPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = static async (context, report) =>
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    durationMs = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description
                })
            });
            await context.Response.WriteAsync(result);
        }
    }).AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ITS API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// For integration test project access
public partial class Program { }
