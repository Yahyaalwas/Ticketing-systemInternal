using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Entities.Content;
using ITS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ITS.Application.Features.Attachments.Commands.AddAttachment;

public sealed class AddAttachmentCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IFileStorageService fileStorage)
    : IRequestHandler<AddAttachmentCommand, AddAttachmentResponse>
{
    public async Task<AddAttachmentResponse> Handle(AddAttachmentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanEditTicketAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to add attachments to this ticket.");

        // Compute SHA-256 checksum
        string checksum;
        using (var sha = SHA256.Create())
        {
            var hashBytes = await sha.ComputeHashAsync(request.FileContent, cancellationToken);
            checksum = Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        // Reset stream for upload
        if (request.FileContent.CanSeek)
            request.FileContent.Seek(0, SeekOrigin.Begin);

        var storageKey = await fileStorage.UploadAsync(
            request.FileContent, request.FileName, request.ContentType, cancellationToken);

        var attachment = Attachment.Create(
            ticket.Id, null, currentUser.UserId,
            request.FileName, storageKey, request.ContentType,
            request.FileSizeBytes, checksum);

        db.Attachments.Add(attachment);

        db.ActivityLogs.Add(ActivityLog.Create(ticket.Id, ticket.ProjectId, currentUser.UserId,
            ActivityType.AttachmentAdded, "Attachment", attachment.Id.ToString(),
            "FileName", null, request.FileName));

        await db.SaveChangesAsync(cancellationToken);

        return new AddAttachmentResponse(attachment.Id, attachment.FileName, fileStorage.GetPublicUrl(storageKey));
    }
}
