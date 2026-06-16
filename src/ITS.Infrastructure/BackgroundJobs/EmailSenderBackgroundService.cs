using ITS.Domain.Enums;
using ITS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace ITS.Infrastructure.BackgroundJobs;

public sealed class EmailSenderBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EmailSenderBackgroundService> logger) : BackgroundService
{
    private const int MaxRetries = 3;
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email sender background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email sender batch failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var pending = await db.EmailQueue
            .Where(e => e.Status == EmailStatus.Pending && e.ScheduledAt <= DateTime.UtcNow && e.AttemptCount < MaxRetries)
            .OrderBy(e => e.ScheduledAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        using var smtpClient = BuildSmtpClient();

        foreach (var email in pending)
        {
            try
            {
                email.MarkSending();
                await db.SaveChangesAsync(cancellationToken);

                var message = new MailMessage
                {
                    From = new MailAddress(
                        configuration["Email:FromAddress"] ?? "its@company.com",
                        configuration["Email:FromName"] ?? "ITS"),
                    Subject = email.Subject,
                    Body = email.HtmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(email.RecipientEmail, email.RecipientName));

                if (!string.IsNullOrEmpty(email.PlainTextBody))
                {
                    message.AlternateViews.Add(
                        AlternateView.CreateAlternateViewFromString(email.PlainTextBody, null, "text/plain"));
                }

                await smtpClient.SendMailAsync(message);

                email.MarkSent();
                logger.LogInformation("Sent email to {Recipient}", email.RecipientEmail);
            }
            catch (Exception ex)
            {
                email.MarkFailed(ex.Message);
                logger.LogWarning(ex, "Failed to send email {Id} to {Recipient}", email.Id, email.RecipientEmail);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private SmtpClient BuildSmtpClient()
    {
        var client = new SmtpClient(
            configuration["Email:SmtpHost"] ?? "localhost",
            configuration.GetValue<int>("Email:SmtpPort", 25))
        {
            EnableSsl = configuration.GetValue<bool>("Email:EnableSsl", false)
        };

        var user = configuration["Email:SmtpUsername"];
        var pass = configuration["Email:SmtpPassword"];
        if (!string.IsNullOrEmpty(user))
            client.Credentials = new NetworkCredential(user, pass);

        return client;
    }
}
