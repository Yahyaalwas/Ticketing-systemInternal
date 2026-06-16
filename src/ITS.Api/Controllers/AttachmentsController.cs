using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Content;
using ITS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/attachments")]
[Authorize]
[Produces("application/json")]
public class AttachmentsController(
    ApplicationDbContext db,
    IFileStorageService fileStorage,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IConfiguration configuration) : ControllerBase
{
    private static readonly long MaxFileSizeBytes = 25L * 1024 * 1024; // 25 MB default

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> GetAttachments(
        Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            return Forbid();

        var attachments = await db.Attachments.AsNoTracking()
            .Where(a => a.TicketId == ticketId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto(
                a.Id, a.FileName, a.ContentType, a.FileSizeBytes,
                fileStorage.GetPublicUrl(a.StorageKey),
                a.ThumbnailKey != null ? fileStorage.GetPublicUrl(a.ThumbnailKey) : null,
                a.UploaderUserId, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(attachments);
    }

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
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var maxBytes = configuration.GetValue<long>("Attachments:MaxFileSizeBytes", MaxFileSizeBytes);
        if (file.Length > maxBytes)
            return BadRequest(new { message = $"File exceeds maximum size of {maxBytes / 1024 / 1024} MB." });

        var allowedTypes = configuration.GetSection("Attachments:AllowedContentTypes").Get<string[]>() ?? [];
        if (allowedTypes.Length > 0 && !allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return BadRequest(new { message = $"Content type '{file.ContentType}' is not allowed." });

        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", ticketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            return Forbid();

        // Compute SHA-256 checksum
        string checksum;
        await using (var stream = file.OpenReadStream())
        {
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Upload file
        string storageKey;
        await using (var stream = file.OpenReadStream())
        {
            storageKey = await fileStorage.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);
        }

        var retentionDays = configuration.GetValue<int>("Attachments:RetentionDays", 90);
        var attachment = Attachment.Create(
            ticketId, null, currentUser.UserId,
            file.FileName, storageKey, file.ContentType,
            file.Length, checksum, retentionDays);

        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new AttachmentDto(
            attachment.Id, attachment.FileName, attachment.ContentType, attachment.FileSizeBytes,
            fileStorage.GetPublicUrl(storageKey), null, currentUser.UserId, attachment.CreatedAt);

        return Created($"api/tickets/{ticketId}/attachments/{attachment.Id}", dto);
    }

    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid ticketId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId && !a.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Attachment", attachmentId);

        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken)!;

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket!.ProjectId, cancellationToken))
            return Forbid();

        var stream = await fileStorage.DownloadAsync(attachment.StorageKey, cancellationToken);
        return File(stream, attachment.ContentType, attachment.FileName);
    }
}

public sealed record AttachmentDto(
    Guid Id, string FileName, string ContentType, long FileSizeBytes,
    string Url, string? ThumbnailUrl, Guid UploaderUserId, DateTime UploadedAt);
