using MediatR;

namespace ITS.Application.Features.Comments.Queries.GetComments;

public sealed record GetCommentsQuery(Guid TicketId, int PageNumber = 1, int PageSize = 50) : IRequest<CommentListResult>;

public sealed record CommentListResult(
    IReadOnlyList<CommentDto> Items,
    int TotalCount,
    int PageNumber,
    int TotalPages);

public sealed record CommentDto(
    Guid Id,
    Guid? ParentCommentId,
    Guid AuthorUserId,
    string AuthorName,
    string? AuthorAvatarUrl,
    string Body,
    string? BodyHtml,
    bool IsEdited,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<Guid> MentionedUserIds);
