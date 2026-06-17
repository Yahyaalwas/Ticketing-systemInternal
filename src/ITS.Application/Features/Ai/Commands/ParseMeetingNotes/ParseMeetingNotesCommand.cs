using MediatR;

namespace ITS.Application.Features.Ai.Commands.ParseMeetingNotes;

public sealed record ParseMeetingNotesCommand(
    string RawNotes,
    string? MeetingTitle = null,
    DateOnly? MeetingDate = null
) : IRequest<MeetingParseResultDto>;

public sealed record MeetingParseResultDto(
    string MeetingTitle,
    IReadOnlyList<ExtractedTask> Tasks,
    IReadOnlyList<ExtractedDecision> Decisions,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Dependencies,
    string NarrativeSummary,
    DateTimeOffset ParsedAt
);

public sealed record ExtractedTask(
    string Title,
    string? Description,
    string? Owner,
    string? DueDate,
    string Priority
);

public sealed record ExtractedDecision(
    string Decision,
    string? Context,
    string? Owner
);
