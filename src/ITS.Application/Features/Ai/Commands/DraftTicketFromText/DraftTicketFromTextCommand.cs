using MediatR;

namespace ITS.Application.Features.Ai.Commands.DraftTicketFromText;

public sealed record DraftTicketFromTextCommand(
    string RawText,
    Guid? ProjectId = null
) : IRequest<TicketDraftDto>;

public sealed record TicketDraftDto(
    string SuggestedTitle,
    string SuggestedDescription,
    string SuggestedPriority,
    string SuggestedIssueType,
    IReadOnlyList<string> SuggestedLabels,
    string? SuggestedAssigneeName,
    string? SuggestedDueDate,
    string ProviderName,
    DateTimeOffset GeneratedAt
);
