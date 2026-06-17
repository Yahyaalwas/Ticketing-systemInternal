using FluentValidation;

namespace ITS.Application.Features.Projects.Commands.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandValidator : AbstractValidator<ChangeMemberRoleCommand>
{
    public ChangeMemberRoleCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRoleId).GreaterThan(0);
    }
}
