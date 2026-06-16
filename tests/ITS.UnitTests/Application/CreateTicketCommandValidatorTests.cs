using FluentAssertions;
using FluentValidation.TestHelper;
using ITS.Application.Features.Tickets.Commands.CreateTicket;

namespace ITS.UnitTests.Application;

public class CreateTicketCommandValidatorTests
{
    private readonly CreateTicketCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_PassesValidation()
    {
        var command = new CreateTicketCommand(
            ProjectId: Guid.NewGuid(),
            Title: "Valid ticket title",
            Description: null,
            IssueTypeId: 1,
            PriorityId: null,
            AssigneeUserId: null,
            ParentTicketId: null,
            EpicTicketId: null,
            DueDate: null,
            StoryPoints: null,
            LabelIds: null,
            CustomFieldValues: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Title_FailsValidation()
    {
        var command = new CreateTicketCommand(
            Guid.NewGuid(), "", null, 1, null, null, null, null, null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_Over500Chars_FailsValidation()
    {
        var command = new CreateTicketCommand(
            Guid.NewGuid(), new string('X', 501), null, 1, null, null, null, null, null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Empty_ProjectId_FailsValidation()
    {
        var command = new CreateTicketCommand(
            Guid.Empty, "Title", null, 1, null, null, null, null, null, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }

    [Fact]
    public void Negative_StoryPoints_FailsValidation()
    {
        var command = new CreateTicketCommand(
            Guid.NewGuid(), "Title", null, 1, null, null, null, null, null,
            StoryPoints: -1, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StoryPoints);
    }

    [Fact]
    public void TooManyLabels_FailsValidation()
    {
        var tooManyLabels = Enumerable.Range(1, 21).ToList();
        var command = new CreateTicketCommand(
            Guid.NewGuid(), "Title", null, 1, null, null, null, null, null, null,
            LabelIds: tooManyLabels, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LabelIds);
    }
}
