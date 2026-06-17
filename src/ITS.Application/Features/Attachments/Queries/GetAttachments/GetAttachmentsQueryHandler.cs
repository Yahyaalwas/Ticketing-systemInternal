using ITS.Application.Common.Exceptions;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Attachments.Queries.GetAttachments;

public sealed class GetAttachmentsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz,
    IFileStorageService fileStorage)
    : IRequestHandler<GetAttachmentsQuery, IReadOnlyList<AttachmentDto>>
{
    public async Task<IReadOnlyList<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && !t.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        if (!await authz.CanViewProjectAsync(currentUser.UserId, ticket.ProjectId, cancellationToken))
            throw new ForbiddenAccessException("You do not have permission to view this ticket.");

        var attachments = await db.Attachments.AsNoTracking()
            .Where(a => a.TicketId == request.TicketId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        if (attachments.Count == 0) return [];

        var uploaderIds = attachments.Select(a => a.UploaderUserId).Distinct().ToList();
        var uploaders = await db.Users.AsNoTracking()
            .Where(u => uploaderIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        return attachments.Select(a => new AttachmentDto(
            a.Id, a.FileName, a.ContentType, a.FileSizeBytes, a.Checksum,
            fileStorage.GetPublicUrl(a.StorageKey),
            a.UploaderUserId,
            uploaders.GetValueOrDefault(a.UploaderUserId, "Unknown"),
            a.CreatedAt)).ToList().AsReadOnly();
    }
}
