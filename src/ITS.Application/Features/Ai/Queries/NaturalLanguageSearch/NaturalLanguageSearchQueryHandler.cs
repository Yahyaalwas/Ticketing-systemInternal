using System.Text.Json;
using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Queries.NaturalLanguageSearch;

public sealed class NaturalLanguageSearchQueryHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiCacheService cache,
    IAiAuditService audit,
    IAiRateLimiter rateLimiter,
    ICurrentUserService currentUser)
    : IRequestHandler<NaturalLanguageSearchQuery, NaturalLanguageSearchResult>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<NaturalLanguageSearchResult> Handle(NaturalLanguageSearchQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "nl-search", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var cacheKey = $"ai:nlsearch:{userId}:{request.UserQuery.GetHashCode():X}";
        var cached = await cache.GetAsync(cacheKey, ct);
        ParsedFiltersJson? filters = null;
        string interpretation = "";

        if (cached is not null)
        {
            var cachedObj = JsonSerializer.Deserialize<CachedSearch>(cached, JsonOpts);
            if (cachedObj is not null) { filters = cachedObj.Filters; interpretation = cachedObj.Interpretation; }
        }

        if (filters is null)
        {
            const string systemPrompt = """
                You translate natural language ticket queries into structured JSON filters. Return ONLY valid JSON:
                {
                  "interpretation": "Human-readable explanation of what will be shown",
                  "assigneeName": "name or null",
                  "priorityName": "Critical|High|Medium|Low or null",
                  "statusName": "status name or null",
                  "issueTypeName": "Bug|Feature|Task|Improvement|Question|Incident or null",
                  "departmentName": "department or null",
                  "labelName": "label or null",
                  "overdueOnly": true/false or null,
                  "unassignedOnly": true/false or null,
                  "freeTextSearch": "keywords or null"
                }
                """;

            var start = DateTimeOffset.UtcNow;
            var aiResp = await ai.CompleteAsync(
                new AiTextRequest(systemPrompt, request.UserQuery, MaxTokens: 400, Temperature: 0.1f), ct);
            var latency = DateTimeOffset.UtcNow - start;

            await audit.LogAsync(new AiAuditEntry("nl-search", userId, aiResp.ProviderName,
                aiResp.PromptTokens, aiResp.CompletionTokens, latency, false, null, start), ct);

            try { filters = JsonSerializer.Deserialize<ParsedFiltersJson>(aiResp.Content, JsonOpts) ?? new(); }
            catch { filters = new ParsedFiltersJson { FreeTextSearch = request.UserQuery }; }

            interpretation = filters.Interpretation ?? $"Searching for: {request.UserQuery}";

            await cache.SetAsync(cacheKey, JsonSerializer.Serialize(new CachedSearch(filters, interpretation)), CacheTtl, ct);
        }

        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.Tickets.AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (request.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == request.ProjectId.Value);

        if (!string.IsNullOrWhiteSpace(filters.AssigneeName))
            query = query.Where(t => t.Assignee != null && t.Assignee.DisplayName.Contains(filters.AssigneeName));

        if (!string.IsNullOrWhiteSpace(filters.PriorityName))
            query = query.Where(t => t.Priority != null && t.Priority.Name == filters.PriorityName);

        if (!string.IsNullOrWhiteSpace(filters.StatusName))
            query = query.Where(t => t.Status!.Name == filters.StatusName);

        if (!string.IsNullOrWhiteSpace(filters.IssueTypeName))
            query = query.Where(t => t.IssueType!.Name == filters.IssueTypeName);

        if (!string.IsNullOrWhiteSpace(filters.LabelName))
            query = query.Where(t => t.Labels.Any(l => l.Name == filters.LabelName));

        if (filters.OverdueOnly == true)
            query = query.Where(t => t.DueDate != null && t.DueDate < now);

        if (filters.UnassignedOnly == true)
            query = query.Where(t => t.AssigneeUserId == null);

        if (!string.IsNullOrWhiteSpace(filters.FreeTextSearch))
        {
            var kw = filters.FreeTextSearch;
            query = query.Where(t => t.Title.Contains(kw) || (t.Description != null && t.Description.Contains(kw)));
        }

        var hits = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Take(50)
            .Select(t => new TicketSearchHit(
                t.Id,
                t.TicketKey,
                t.Title,
                t.Status!.Name,
                t.Priority != null ? t.Priority.Name : null,
                t.Assignee != null ? t.Assignee.DisplayName : null,
                t.DueDate,
                t.DueDate != null && t.DueDate < now))
            .ToListAsync(ct);

        return new NaturalLanguageSearchResult(interpretation,
            new ParsedSearchFilters(filters.AssigneeName, filters.PriorityName, filters.StatusName,
                filters.IssueTypeName, filters.DepartmentName, filters.LabelName,
                filters.OverdueOnly, filters.UnassignedOnly, filters.FreeTextSearch),
            hits);
    }

    private sealed class ParsedFiltersJson
    {
        public string? Interpretation { get; set; }
        public string? AssigneeName { get; set; }
        public string? PriorityName { get; set; }
        public string? StatusName { get; set; }
        public string? IssueTypeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? LabelName { get; set; }
        public bool? OverdueOnly { get; set; }
        public bool? UnassignedOnly { get; set; }
        public string? FreeTextSearch { get; set; }
    }

    private sealed record CachedSearch(ParsedFiltersJson Filters, string Interpretation);
}
