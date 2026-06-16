using MediatR;

namespace ITS.Application.Features.Comments.Commands.AddComment;

public sealed record AddCommentCommand(
    Guid TicketId,
    Guid? ParentCommentId,
    string Body
) : IRequest<AddCommentResponse>;

public sealed record AddCommentResponse(Guid CommentId, string BodyHtml);
