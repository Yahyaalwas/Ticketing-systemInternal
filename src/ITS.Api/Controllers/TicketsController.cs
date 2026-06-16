using ITS.Application.Features.Comments.Commands.AddComment;
using ITS.Application.Features.Tickets.Commands.CreateTicket;
using ITS.Application.Features.Tickets.Commands.DeleteTicket;
using ITS.Application.Features.Tickets.Commands.TransitionTicket;
using ITS.Application.Features.Tickets.Queries.GetKanbanBoard;
using ITS.Application.Features.Tickets.Queries.GetTicket;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
[Produces("application/json")]
public class TicketsController(ISender mediator) : ControllerBase
{
    /// <summary>Get full ticket details including links, watchers, and counts.</summary>
    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(
        Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTicketQuery(ticketId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a new ticket in a project.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateTicketResponse>> CreateTicket(
        [FromBody] CreateTicketCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = result.TicketId }, result);
    }

    /// <summary>Transition a ticket to a new status via a workflow transition.</summary>
    [HttpPost("{ticketId:guid}/transitions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransitionTicket(
        Guid ticketId,
        [FromBody] TransitionTicketRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        var command = new TransitionTicketCommand(
            ticketId, request.ToStatusId, request.Comment, request.ResolutionId, rowVersion);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Soft-delete a ticket.</summary>
    [HttpDelete("{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTicketCommand(ticketId), cancellationToken);
        return NoContent();
    }

    /// <summary>Add a comment to a ticket.</summary>
    [HttpPost("{ticketId:guid}/comments")]
    [ProducesResponseType(typeof(AddCommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddCommentResponse>> AddComment(
        Guid ticketId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddCommentCommand(ticketId, request.ParentCommentId, request.Body);
        var result = await mediator.Send(command, cancellationToken);
        return Created($"api/tickets/{ticketId}/comments/{result.CommentId}", result);
    }

    /// <summary>Get the Kanban board for a project.</summary>
    [HttpGet("kanban/{projectId:guid}")]
    [ProducesResponseType(typeof(KanbanBoardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KanbanBoardDto>> GetKanbanBoard(
        Guid projectId,
        [FromQuery] Guid? assigneeUserId,
        [FromQuery] int? priorityId,
        [FromQuery] int? labelId,
        [FromQuery] Guid? epicTicketId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetKanbanBoardQuery(projectId, assigneeUserId, priorityId, labelId, epicTicketId),
            cancellationToken);
        return Ok(result);
    }
}

public sealed record TransitionTicketRequest(int ToStatusId, string? Comment, int? ResolutionId);
public sealed record AddCommentRequest(Guid? ParentCommentId, string Body);
