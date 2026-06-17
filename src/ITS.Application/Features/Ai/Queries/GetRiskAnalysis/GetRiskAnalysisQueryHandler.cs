using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.GetRiskAnalysis;

public sealed class GetRiskAnalysisQueryHandler(
    IApplicationDbContext db,
    IAiCacheService cache,
    ICurrentUserService currentUser)
    : IRequestHandler<GetRiskAnalysisQuery, RiskAnalysisDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    public async Task<RiskAnalysisDto> Handle(GetRiskAnalysisQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var inactivityThreshold = now.AddDays(-7);

        var ticketQuery = db.Tickets.AsNoTracking()
            .Where(t => !t.IsDeleted && t.Status!.Category != "Done");

        if (request.ProjectId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.ProjectId == request.ProjectId.Value);
        if (request.TicketId.HasValue)
            ticketQuery = ticketQuery.Where(t => t.Id == request.TicketId.Value);

        var tickets = await ticketQuery
            .Select(t => new
            {
                t.Id, t.TicketKey, t.Title,
                t.AssigneeUserId, t.DueDate,
                t.UpdatedAt, t.ReopenCount, t.IsSlaBreached,
                t.SlaBreachAt, t.CreatedAt,
                Priority = t.Priority != null ? t.Priority.Name : "None",
                HasBlockers = t.LinkedTickets.Any(l => l.LinkType == "blocks")
            })
            .ToListAsync(ct);

        var risks = new List<TicketRisk>();

        foreach (var t in tickets)
        {
            if (t.AssigneeUserId == null)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "MissingAssignee",
                    "Ticket has no assignee and may be overlooked.", "High"));

            if (t.DueDate == null)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "MissingDueDate",
                    "No due date set — delivery timeline is unclear.", "Medium"));

            if (t.DueDate.HasValue && t.DueDate < today)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "Overdue",
                    $"Ticket is past due date ({t.DueDate:yyyy-MM-dd}).", "Critical"));

            if (t.UpdatedAt < inactivityThreshold)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "Inactive",
                    $"No activity for {(now - t.UpdatedAt).Days} days.", "Medium"));

            if (t.ReopenCount >= 3)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "FrequentlyReopened",
                    $"Reopened {t.ReopenCount} times — root cause may not be resolved.", "High"));

            if (t.IsSlaBreached)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "SlaBreached",
                    "SLA has already been breached.", "Critical"));
            else if (t.SlaBreachAt.HasValue && t.SlaBreachAt.Value < now.AddHours(4))
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "SlaNearBreach",
                    $"SLA will breach at {t.SlaBreachAt:g} (within 4 hours).", "High"));

            if (t.HasBlockers)
                risks.Add(new TicketRisk(t.Id, t.TicketKey, t.Title, "Blocked",
                    "Ticket is blocked by another ticket.", "High"));
        }

        var criticalCount = risks.Count(r => r.Severity == "Critical");
        var highCount = risks.Count(r => r.Severity == "High");
        var score = criticalCount * 10 + highCount * 5 + risks.Count(r => r.Severity == "Medium") * 2;
        var level = score >= 20 ? "Critical" : score >= 10 ? "High" : score >= 5 ? "Medium" : "Low";

        return new RiskAnalysisDto([.. risks.OrderBy(r => r.Severity switch { "Critical" => 0, "High" => 1, "Medium" => 2, _ => 3 })],
            score, level, DateTimeOffset.UtcNow);
    }
}
