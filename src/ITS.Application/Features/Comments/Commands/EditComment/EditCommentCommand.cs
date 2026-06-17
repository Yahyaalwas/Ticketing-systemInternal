using MediatR;

namespace ITS.Application.Features.Comments.Commands.EditComment;

public sealed record EditCommentCommand(Guid CommentId, string Body) : IRequest;
