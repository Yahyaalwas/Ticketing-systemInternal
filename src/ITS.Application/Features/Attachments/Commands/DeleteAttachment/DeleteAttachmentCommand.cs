using MediatR;

namespace ITS.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid TicketId, Guid AttachmentId) : IRequest;
