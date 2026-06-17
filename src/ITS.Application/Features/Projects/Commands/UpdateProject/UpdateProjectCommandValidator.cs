using FluentValidation;

namespace ITS.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.LeadUserId).NotEmpty();
        RuleFor(x => x.AvatarUrl).MaximumLength(512).When(x => x.AvatarUrl is not null);
        RuleFor(x => x.RowVersion).NotNull().NotEmpty();
    }
}
