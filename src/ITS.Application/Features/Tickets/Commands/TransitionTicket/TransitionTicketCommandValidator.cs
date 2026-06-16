using FluentValidation;

namespace ITS.Application.Features.Tickets.Commands.TransitionTicket;

public sealed class TransitionTicketCommandValidator : AbstractValidator<TransitionTicketCommand>
{
    public TransitionTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("Ticket ID is required.");

        RuleFor(x => x.ToStatusId)
            .GreaterThan(0).WithMessage("Target status is required.");

        RuleFor(x => x.Comment)
            .MaximumLength(65536).WithMessage("Comment is too long.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required for concurrency control.");
    }
}
