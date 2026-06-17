using MediatR;

namespace ITS.Application.Features.Attachments.Queries.GetAttachments;

public sealed record GetAttachmentsQuery(Guid TicketId) : IRequest<IReadOnlyList<AttachmentDto>>;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? Checksum,
    string PublicUrl,
    Guid UploaderUserId,
    string UploaderName,
    DateTime UploadedAt);
