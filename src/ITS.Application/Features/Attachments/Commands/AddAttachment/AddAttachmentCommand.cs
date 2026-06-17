using MediatR;

namespace ITS.Application.Features.Attachments.Commands.AddAttachment;

public sealed record AddAttachmentCommand(
    Guid TicketId,
    string FileName,
    string ContentType,
    Stream FileContent,
    long FileSizeBytes
) : IRequest<AddAttachmentResponse>;

public sealed record AddAttachmentResponse(Guid AttachmentId, string FileName, string PublicUrl);
