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

        var ticket = await db.Tickets
            .AsNoTracking()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Title, Status = t.Status!.Name, Priority = t.Priority != null ? t.Priority.Name : "None" })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Ticket {request.TicketId} not found.");

        var recentComments = await db.Comments
            .AsNoTracking()
            .Where(c => c.TicketId == request.TicketId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new { Author = c.Author.DisplayName, c.Body })
            .ToListAsync(ct);

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
