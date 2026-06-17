using FluentValidation;

namespace ITS.Application.Features.Tickets.Commands.UpdateTicket;

public sealed class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(100_000).When(x => x.Description is not null);
        RuleFor(x => x.IssueTypeId).GreaterThan(0);
        RuleFor(x => x.StoryPoints).GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue);
        RuleFor(x => x.EstimatedHours).GreaterThanOrEqualTo(0).When(x => x.EstimatedHours.HasValue);
        RuleFor(x => x.RowVersion).NotNull().NotEmpty();
    }
}
