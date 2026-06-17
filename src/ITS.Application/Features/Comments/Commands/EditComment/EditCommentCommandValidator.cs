using FluentValidation;

namespace ITS.Application.Features.Comments.Commands.EditComment;

public sealed class EditCommentCommandValidator : AbstractValidator<EditCommentCommand>
{
    public EditCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(100_000);
    }
}
