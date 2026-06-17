using ITS.Application.Features.Ai.Commands.DraftTicketFromText;
using ITS.Application.Features.Ai.Commands.GenerateComment;
using ITS.Application.Features.Ai.Commands.ParseMeetingNotes;
using ITS.Application.Features.Ai.Commands.SummarizeTicket;
using ITS.Application.Features.Ai.Queries.AnswerKnowledgeQuestion;
using ITS.Application.Features.Ai.Queries.FindDuplicates;
using ITS.Application.Features.Ai.Queries.GetExecutiveReport;
using ITS.Application.Features.Ai.Queries.GetRiskAnalysis;
using ITS.Application.Features.Ai.Queries.GetSimilarTickets;
using ITS.Application.Features.Ai.Queries.GetSprintIntelligence;
using ITS.Application.Features.Ai.Queries.NaturalLanguageSearch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
[Produces("application/json")]
public class AiController(ISender mediator) : ControllerBase
{
    /// <summary>Generate an AI summary for a ticket (executive summary, blockers, action items, etc.)</summary>
    [HttpPost("tickets/{ticketId:guid}/summarize")]
    [ProducesResponseType(typeof(TicketAiSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketAiSummaryDto>> SummarizeTicket(
        Guid ticketId,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new SummarizeTicketCommand(ticketId, forceRefresh), cancellationToken));

    /// <summary>Generate an AI-drafted comment for a ticket. Never auto-posts — always requires user approval.</summary>
    [HttpPost("tickets/{ticketId:guid}/generate-comment")]
    [ProducesResponseType(typeof(GeneratedCommentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GeneratedCommentDto>> GenerateComment(
        Guid ticketId,
        [FromBody] GenerateCommentRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(
            new GenerateCommentCommand(ticketId, request.Instruction, request.Tone),
            cancellationToken));

    /// <summary>Parse meeting notes into structured tasks, decisions, risks, and dependencies.</summary>
    [HttpPost("parse-meeting-notes")]
    [ProducesResponseType(typeof(MeetingParseResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeetingParseResultDto>> ParseMeetingNotes(
        [FromBody] ParseMeetingNotesRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(
            new ParseMeetingNotesCommand(request.RawNotes, request.MeetingTitle, request.MeetingDate),
            cancellationToken));

    /// <summary>Draft a ticket from raw text (email, meeting notes, description).</summary>
    [HttpPost("draft-ticket")]
    [ProducesResponseType(typeof(TicketDraftDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketDraftDto>> DraftTicket(
        [FromBody] DraftTicketRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new DraftTicketFromTextCommand(request.RawText, request.ProjectId), cancellationToken));

    /// <summary>Natural language ticket search. Translates plain-English queries to structured filters.</summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(NaturalLanguageSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<NaturalLanguageSearchResult>> NaturalLanguageSearch(
        [FromBody] NaturalLanguageSearchRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new NaturalLanguageSearchQuery(request.Query, request.ProjectId), cancellationToken));

    /// <summary>Detect potential duplicate tickets before creating a new one.</summary>
    [HttpPost("find-duplicates")]
    [ProducesResponseType(typeof(DuplicateDetectionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<DuplicateDetectionResult>> FindDuplicates(
        [FromBody] FindDuplicatesRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(
            new FindDuplicatesQuery(request.Title, request.Description, request.ProjectId),
            cancellationToken));

    /// <summary>Get semantically similar tickets for context when viewing a ticket.</summary>
    [HttpGet("tickets/{ticketId:guid}/similar")]
    [ProducesResponseType(typeof(SimilarTicketsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SimilarTicketsDto>> GetSimilarTickets(
        Guid ticketId,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetSimilarTicketsQuery(ticketId), cancellationToken));

    /// <summary>Generate an AI-powered executive report for a period.</summary>
    [HttpGet("executive-report")]
    [ProducesResponseType(typeof(ExecutiveReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExecutiveReportDto>> GetExecutiveReport(
        [FromQuery] ReportPeriod period = ReportPeriod.Weekly,
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool forceRefresh = false,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(
            new GetExecutiveReportQuery(period, projectId, departmentId, forceRefresh),
            cancellationToken));

    /// <summary>Get sprint-level AI intelligence: at-risk tickets, workload imbalances, SLA warnings.</summary>
    [HttpGet("sprint-intelligence/{projectId:guid}")]
    [ProducesResponseType(typeof(SprintIntelligenceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SprintIntelligenceDto>> GetSprintIntelligence(
        Guid projectId,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetSprintIntelligenceQuery(projectId), cancellationToken));

    /// <summary>Answer natural language knowledge questions using ticket data as context.</summary>
    [HttpPost("knowledge-assistant")]
    [ProducesResponseType(typeof(KnowledgeAnswerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KnowledgeAnswerDto>> AnswerQuestion(
        [FromBody] KnowledgeQuestionRequest request,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new AnswerKnowledgeQuestionQuery(request.Question, request.ProjectId), cancellationToken));

    /// <summary>Rule-based risk analysis for tickets in a project or a specific ticket.</summary>
    [HttpGet("risk-analysis")]
    [ProducesResponseType(typeof(RiskAnalysisDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RiskAnalysisDto>> GetRiskAnalysis(
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? ticketId = null,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetRiskAnalysisQuery(projectId, ticketId), cancellationToken));
}

// ── Request body DTOs ────────────────────────────────────────────────────────

public sealed record GenerateCommentRequest(string Instruction, CommentTone Tone = CommentTone.Professional);
public sealed record ParseMeetingNotesRequest(string RawNotes, string? MeetingTitle = null, DateOnly? MeetingDate = null);
public sealed record DraftTicketRequest(string RawText, Guid? ProjectId = null);
public sealed record NaturalLanguageSearchRequest(string Query, Guid? ProjectId = null);
public sealed record FindDuplicatesRequest(string Title, string? Description, Guid ProjectId);
public sealed record KnowledgeQuestionRequest(string Question, Guid? ProjectId = null);
