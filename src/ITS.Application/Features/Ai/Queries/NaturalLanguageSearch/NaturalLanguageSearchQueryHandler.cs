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

        // Pre-resolve filter names to IDs for efficient FK-based filtering
        int[]? matchingStatusIds = null;
        if (!string.IsNullOrWhiteSpace(filters.StatusName))
            matchingStatusIds = await db.WorkflowStatuses.AsNoTracking()
                .Where(s => s.Name == filters.StatusName)
                .Select(s => s.Id).ToArrayAsync(ct);

        int[]? matchingPriorityIds = null;
        if (!string.IsNullOrWhiteSpace(filters.PriorityName))
            matchingPriorityIds = await db.Priorities.AsNoTracking()
                .Where(p => p.Name == filters.PriorityName)
                .Select(p => p.Id).ToArrayAsync(ct);

        int[]? matchingIssueTypeIds = null;
        if (!string.IsNullOrWhiteSpace(filters.IssueTypeName))
            matchingIssueTypeIds = await db.IssueTypes.AsNoTracking()
                .Where(it => it.Name == filters.IssueTypeName)
                .Select(it => it.Id).ToArrayAsync(ct);

        Guid[]? matchingAssigneeIds = null;
        if (!string.IsNullOrWhiteSpace(filters.AssigneeName))
            matchingAssigneeIds = await db.Users.AsNoTracking()
                .Where(u => u.DisplayName.Contains(filters.AssigneeName))
                .Select(u => u.Id).ToArrayAsync(ct);

        int[]? matchingLabelIds = null;
        if (!string.IsNullOrWhiteSpace(filters.LabelName))
            matchingLabelIds = await db.Labels.AsNoTracking()
                .Where(l => l.Name == filters.LabelName)
                .Select(l => l.Id).ToArrayAsync(ct);

        var query = db.Tickets.AsNoTracking()
            .Where(t => !t.IsDeleted);

        if (request.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == request.ProjectId.Value);

        if (matchingAssigneeIds is not null)
            query = query.Where(t => t.AssigneeUserId.HasValue && matchingAssigneeIds.Contains(t.AssigneeUserId.Value));

        if (matchingPriorityIds is not null)
            query = query.Where(t => t.PriorityId.HasValue && matchingPriorityIds.Contains(t.PriorityId.Value));

        if (matchingStatusIds is not null)
            query = query.Where(t => matchingStatusIds.Contains(t.StatusId));

        if (matchingIssueTypeIds is not null)
            query = query.Where(t => matchingIssueTypeIds.Contains(t.IssueTypeId));

        if (matchingLabelIds is not null)
            query = query.Where(t => t.Labels.Any(l => matchingLabelIds.Contains(l.LabelId)));

        if (filters.OverdueOnly == true)
            query = query.Where(t => t.DueDate != null && t.DueDate < now);

        if (filters.UnassignedOnly == true)
            query = query.Where(t => t.AssigneeUserId == null);

        if (!string.IsNullOrWhiteSpace(filters.FreeTextSearch))
        {
            var kw = filters.FreeTextSearch;
            query = query.Where(t => t.Title.Contains(kw) || (t.Description != null && t.Description.Contains(kw)));
        }

        var hitsRaw = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Take(50)
            .Select(t => new { t.Id, t.TicketNumber, t.ProjectId, t.Title, t.StatusId, t.PriorityId, t.AssigneeUserId, t.DueDate })
            .ToListAsync(ct);

        // Bulk-load reference data for projection
        var hitStatusIds = hitsRaw.Select(t => t.StatusId).Distinct().ToList();
        var hitPriorityIds = hitsRaw.Where(t => t.PriorityId.HasValue).Select(t => t.PriorityId!.Value).Distinct().ToList();
        var hitAssigneeIds = hitsRaw.Where(t => t.AssigneeUserId.HasValue).Select(t => t.AssigneeUserId!.Value).Distinct().ToList();
        var hitProjectIds = hitsRaw.Select(t => t.ProjectId).Distinct().ToList();

        var hitStatusMap = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => hitStatusIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var hitPriorityMap = await db.Priorities.AsNoTracking()
            .Where(p => hitPriorityIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var hitAssigneeMap = await db.Users.AsNoTracking()
            .Where(u => hitAssigneeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        var hitProjectMap = await db.Projects.AsNoTracking()
            .Where(p => hitProjectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProjectKey, ct);

        var hits = hitsRaw.Select(t => new TicketSearchHit(
            t.Id,
            $"{hitProjectMap.GetValueOrDefault(t.ProjectId, "")}-{t.TicketNumber}",
            t.Title,
            hitStatusMap.GetValueOrDefault(t.StatusId, "Unknown"),
            t.PriorityId.HasValue ? hitPriorityMap.GetValueOrDefault(t.PriorityId.Value) : null,
            t.AssigneeUserId.HasValue ? hitAssigneeMap.GetValueOrDefault(t.AssigneeUserId.Value) : null,
            t.DueDate,
            t.DueDate != null && t.DueDate < now)).ToList();

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
