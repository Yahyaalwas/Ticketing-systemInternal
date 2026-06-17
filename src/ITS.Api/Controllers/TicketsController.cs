using ITS.Application.Features.Comments.Commands.AddComment;
using ITS.Application.Features.Comments.Commands.DeleteComment;
using ITS.Application.Features.Comments.Commands.EditComment;
using ITS.Application.Features.Comments.Queries.GetComments;
using ITS.Application.Features.Tickets.Commands.AddLabel;
using ITS.Application.Features.Tickets.Commands.AddWatcher;
using ITS.Application.Features.Tickets.Commands.AssignTicket;
using ITS.Application.Features.Tickets.Commands.ChangeDueDate;
using ITS.Application.Features.Tickets.Commands.ChangePriority;
using ITS.Application.Features.Tickets.Commands.CreateTicket;
using ITS.Application.Features.Tickets.Commands.DeleteTicket;
using ITS.Application.Features.Tickets.Commands.RemoveLabel;
using ITS.Application.Features.Tickets.Commands.RemoveWatcher;
using ITS.Application.Features.Tickets.Commands.TransitionTicket;
using ITS.Application.Features.Tickets.Commands.UpdateTicket;
using ITS.Application.Features.Tickets.Queries.GetKanbanBoard;
using ITS.Application.Features.Tickets.Queries.GetTicket;
using ITS.Application.Features.Tickets.Queries.ListTickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class TicketsController(ISender mediator) : ControllerBase
{
    /// <summary>Get full ticket details including links, watchers, and counts.</summary>
    [HttpGet("tickets/{ticketId:guid}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketDetailDto>> GetTicket(
        Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTicketQuery(ticketId), cancellationToken);
        return Ok(result);
    }

    /// <summary>List tickets with optional filters and pagination.</summary>
    [HttpGet("tickets")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTickets(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? assigneeUserId,
        [FromQuery] int? priorityId,
        [FromQuery] int? issueTypeId,
        [FromQuery] int? statusId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListTicketsQuery(projectId, assigneeUserId, priorityId, issueTypeId, statusId, search, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a new ticket in a project.</summary>
    [HttpPost("tickets")]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateTicketResponse>> CreateTicket(
        [FromBody] CreateTicketCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = result.TicketId }, result);
    }

    /// <summary>Update an existing ticket. Requires If-Match header for optimistic concurrency.</summary>
    [HttpPut("tickets/{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTicket(
        Guid ticketId,
        [FromBody] UpdateTicketRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        var command = new UpdateTicketCommand(
            ticketId, request.Title, request.Description, request.IssueTypeId,
            request.PriorityId, request.AssigneeUserId, request.DueDate,
            request.StoryPoints, request.EstimatedHours, rowVersion);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Transition a ticket to a new status via a workflow transition.</summary>
    [HttpPost("tickets/{ticketId:guid}/transitions")]
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

    /// <summary>Assign (or unassign) a ticket. Requires If-Match header.</summary>
    [HttpPatch("tickets/{ticketId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTicket(
        Guid ticketId,
        [FromBody] AssignTicketRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        await mediator.Send(new AssignTicketCommand(ticketId, request.AssigneeUserId, rowVersion), cancellationToken);
        return NoContent();
    }

    /// <summary>Change the priority of a ticket. Requires If-Match header.</summary>
    [HttpPatch("tickets/{ticketId:guid}/priority")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePriority(
        Guid ticketId,
        [FromBody] ChangePriorityRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        await mediator.Send(new ChangePriorityCommand(ticketId, request.PriorityId, rowVersion), cancellationToken);
        return NoContent();
    }

    /// <summary>Change the due date of a ticket. Requires If-Match header.</summary>
    [HttpPatch("tickets/{ticketId:guid}/due-date")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeDueDate(
        Guid ticketId,
        [FromBody] ChangeDueDateRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        await mediator.Send(new ChangeDueDateCommand(ticketId, request.DueDate, rowVersion), cancellationToken);
        return NoContent();
    }

    /// <summary>Soft-delete a ticket.</summary>
    [HttpDelete("tickets/{ticketId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTicketCommand(ticketId), cancellationToken);
        return NoContent();
    }

    /// <summary>Add a label to a ticket.</summary>
    [HttpPost("tickets/{ticketId:guid}/labels/{labelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddLabel(
        Guid ticketId, int labelId, CancellationToken cancellationToken)
    {
        await mediator.Send(new AddLabelCommand(ticketId, labelId), cancellationToken);
        return NoContent();
    }

    /// <summary>Remove a label from a ticket.</summary>
    [HttpDelete("tickets/{ticketId:guid}/labels/{labelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLabel(
        Guid ticketId, int labelId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveLabelCommand(ticketId, labelId), cancellationToken);
        return NoContent();
    }

    /// <summary>Add a watcher to a ticket.</summary>
    [HttpPost("tickets/{ticketId:guid}/watchers/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddWatcher(
        Guid ticketId, Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new AddWatcherCommand(ticketId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Remove a watcher from a ticket.</summary>
    [HttpDelete("tickets/{ticketId:guid}/watchers/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWatcher(
        Guid ticketId, Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveWatcherCommand(ticketId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Add a comment to a ticket.</summary>
    [HttpPost("tickets/{ticketId:guid}/comments")]
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

    /// <summary>List comments for a ticket with pagination.</summary>
    [HttpGet("tickets/{ticketId:guid}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComments(
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCommentsQuery(ticketId, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Edit an existing comment.</summary>
    [HttpPut("tickets/{ticketId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EditComment(
        Guid ticketId,
        Guid commentId,
        [FromBody] EditCommentBodyRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new EditCommentCommand(ticketId, commentId, request.Body), cancellationToken);
        return NoContent();
    }

    /// <summary>Delete a comment.</summary>
    [HttpDelete("tickets/{ticketId:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteComment(
        Guid ticketId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCommentCommand(ticketId, commentId), cancellationToken);
        return NoContent();
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
        [FromQuery] int? issueTypeId,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetKanbanBoardQuery(projectId, assigneeUserId, priorityId, labelId, epicTicketId, issueTypeId, search),
            cancellationToken);
        return Ok(result);
    }
}

public sealed record TransitionTicketRequest(int ToStatusId, string? Comment, int? ResolutionId);
public sealed record AddCommentRequest(Guid? ParentCommentId, string Body);
public sealed record UpdateTicketRequest(
    string Title,
    string? Description,
    int IssueTypeId,
    int? PriorityId,
    Guid? AssigneeUserId,
    DateOnly? DueDate,
    decimal? StoryPoints,
    decimal? EstimatedHours);
public sealed record AssignTicketRequest(Guid? AssigneeUserId);
public sealed record ChangePriorityRequest(int? PriorityId);
public sealed record ChangeDueDateRequest(DateOnly? DueDate);
public sealed record EditCommentBodyRequest(string Body);
