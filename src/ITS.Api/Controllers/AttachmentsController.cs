using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Application.Features.Attachments.Commands.AddAttachment;
using ITS.Application.Features.Attachments.Commands.DeleteAttachment;
using ITS.Application.Features.Attachments.Queries.GetAttachments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/attachments")]
[Authorize]
[Produces("application/json")]
public class AttachmentsController(
    ISender mediator,
    IFileStorageService fileStorage,
    IApplicationDbContext db) : ControllerBase
{
    /// <summary>List all non-deleted attachments for a ticket.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetAttachments(
        Guid ticketId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAttachmentsQuery(ticketId), cancellationToken);
        return Ok(result);
    }

    /// <summary>Upload a file attachment to a ticket.</summary>
    [HttpPost]
    [RequestSizeLimit(26_214_400)] // 25 MB + overhead
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AttachmentDto>> Upload(
        Guid ticketId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddAttachmentCommand(ticketId, file), cancellationToken);
        return Created($"api/tickets/{ticketId}/attachments/{result.Id}", result);
    }

    /// <summary>Delete an attachment from a ticket.</summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAttachment(
        Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAttachmentCommand(ticketId, attachmentId), cancellationToken);
        return NoContent();
    }

    /// <summary>Download a file attachment, streaming directly from storage.</summary>
    [HttpGet("{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Download(
        Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Attachment", attachmentId);

        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException("Ticket", ticketId);

        var stream = await fileStorage.DownloadAsync(attachment.StorageKey, cancellationToken);
        return File(stream, attachment.ContentType, attachment.FileName);
    }
}
