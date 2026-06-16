using FluentValidation;

namespace ITS.Application.Features.Tickets.Commands.CreateTicket;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(65536).WithMessage("Description is too long.");

        RuleFor(x => x.IssueTypeId)
            .GreaterThan(0).WithMessage("Issue type is required.");

        RuleFor(x => x.StoryPoints)
            .GreaterThanOrEqualTo(0).When(x => x.StoryPoints.HasValue)
            .WithMessage("Story points cannot be negative.");

        RuleFor(x => x.LabelIds)
            .Must(ids => ids == null || ids.Count <= 20)
            .WithMessage("Maximum 20 labels per ticket.");

        RuleFor(x => x.CustomFieldValues)
            .Must(cfv => cfv == null || cfv.Count <= 50)
            .WithMessage("Too many custom field values.");
    }
}
