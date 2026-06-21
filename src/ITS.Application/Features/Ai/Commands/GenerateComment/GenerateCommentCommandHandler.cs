using ITS.Application.Common.Interfaces;
using ITS.Application.Common.Models.Ai;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Ai.Commands.GenerateComment;

public sealed class GenerateCommentCommandHandler(
    IApplicationDbContext db,
    IAiProvider ai,
    IAiRateLimiter rateLimiter,
    IAiAuditService audit,
    IAiDataMasker masker,
    ICurrentUserService currentUser)
    : IRequestHandler<GenerateCommentCommand, GeneratedCommentDto>
{
    public async Task<GeneratedCommentDto> Handle(GenerateCommentCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId.ToString();
        if (!await rateLimiter.IsAllowedAsync(userId, "generate-comment", ct))
            throw new InvalidOperationException("AI rate limit exceeded.");

        var ticketRaw = await db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Title, t.StatusId, t.PriorityId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        var statusName = await db.WorkflowStatuses.AsNoTracking()
            .Where(s => s.Id == ticketRaw.StatusId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? "Unknown";
        var priorityName = ticketRaw.PriorityId.HasValue
            ? await db.Priorities.AsNoTracking().Where(p => p.Id == ticketRaw.PriorityId.Value).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "None"
            : "None";

        var ticket = new { ticketRaw.Title, Status = statusName, Priority = priorityName };

        var commentsRaw = await db.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new { c.AuthorUserId, c.Body })
            .ToListAsync(ct);

        var commentAuthorIds = commentsRaw.Select(c => c.AuthorUserId).Distinct().ToList();
        var commentAuthors = await db.Users.AsNoTracking()
            .Where(u => commentAuthorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        var recentComments = commentsRaw.Select(c => new
        {
            Author = commentAuthors.GetValueOrDefault(c.AuthorUserId, "Unknown"),
            c.Body
        }).ToList();

        var contextBlock = string.Join("\n", recentComments.Select(c => $"[{c.Author}]: {c.Body}"));

        var toneInstruction = request.Tone switch
        {
            CommentTone.Technical => "Write a precise technical comment suitable for engineers.",
            CommentTone.CustomerFacing => "Write a clear, empathetic comment suitable for customers. Avoid jargon.",
            CommentTone.StatusUpdate => "Write a concise status update comment.",
            CommentTone.FollowUp => "Write a polite follow-up message requesting an update.",
            _ => "Write a professional comment."
        };

        var rawUserPrompt = $"""
            TICKET: {ticket.Title} (Status: {ticket.Status}, Priority: {ticket.Priority})
            RECENT COMMENTS:
            {contextBlock}

            INSTRUCTION: {request.Instruction}
            """;

        var systemPrompt = $"""
            {toneInstruction}
            Keep responses concise (150-300 words). Do not include salutations or sign-offs.
            Return only the comment body text, no preamble.
            """;

        var start = DateTimeOffset.UtcNow;
        var aiResp = await ai.CompleteAsync(
            new AiTextRequest(systemPrompt, masker.Mask(rawUserPrompt), MaxTokens: 500, Temperature: 0.5f,
                CorrelationId: request.TicketId.ToString()), ct);
        var latency = DateTimeOffset.UtcNow - start;

        await audit.LogAsync(new AiAuditEntry("generate-comment", userId, aiResp.ProviderName,
            aiResp.PromptTokens, aiResp.CompletionTokens, latency, false,
            request.TicketId.ToString(), start), ct);

        return new GeneratedCommentDto(aiResp.Content.Trim(), request.Tone.ToString(),
            aiResp.ProviderName, DateTimeOffset.UtcNow);
    }
}
