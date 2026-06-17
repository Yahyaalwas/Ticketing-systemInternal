using MediatR;

namespace ITS.Application.Features.Comments.Commands.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest;
