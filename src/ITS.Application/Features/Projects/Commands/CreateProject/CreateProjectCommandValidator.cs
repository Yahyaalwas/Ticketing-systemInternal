using FluentValidation;

namespace ITS.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("Project key is required.")
            .MaximumLength(10).WithMessage("Project key must be 10 characters or fewer.")
            .Matches("^[A-Z0-9]+$").WithMessage("Project key must contain only uppercase letters and digits.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(256).WithMessage("Project name must be 256 characters or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => x.Description is not null);

        RuleFor(x => x.LeadUserId).NotEmpty();
        RuleFor(x => x.DepartmentId).GreaterThan(0);
    }
}
