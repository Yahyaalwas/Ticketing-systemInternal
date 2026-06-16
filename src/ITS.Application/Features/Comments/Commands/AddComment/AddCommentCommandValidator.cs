using FluentValidation;

namespace ITS.Application.Features.Comments.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(65536).WithMessage("Comment is too long.");
    }
}
