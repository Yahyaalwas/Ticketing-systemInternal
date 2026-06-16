using FluentAssertions;
using ITS.Domain.Entities.Workflow;
using ITS.Domain.Enums;

namespace ITS.UnitTests.Domain;

public class WorkflowTests
{
    [Fact]
    public void Create_Template_WithProjectId_ThrowsArgumentException()
    {
        var act = () => Workflow.Create("Template", null, isTemplate: true, projectId: Guid.NewGuid());

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Template workflows cannot be associated with a project*");
    }

    [Fact]
    public void Create_NonTemplate_WithoutProjectId_ThrowsArgumentException()
    {
        var act = () => Workflow.Create("Project Workflow", null, isTemplate: false, projectId: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Non-template workflows must be associated with a project*");
    }

    [Fact]
    public void AddStatus_AddsToCollection()
    {
        var workflow = Workflow.Create("Test", null, false, Guid.NewGuid());

        var status = workflow.AddStatus("To Do", null, StatusCategory.ToDo, "#DFE1E6", 0, true, false);

        workflow.Statuses.Should().Contain(status);
        status.WorkflowId.Should().Be(workflow.Id);
        status.IsInitial.Should().BeTrue();
    }

    [Fact]
    public void CanTransition_WithValidTransition_ReturnsTrue()
    {
        var projectId = Guid.NewGuid();
        var workflow = Workflow.Create("Test", null, false, projectId);
        var todo = workflow.AddStatus("To Do", null, StatusCategory.ToDo, null, 0, true, false);
        var inProgress = workflow.AddStatus("In Progress", null, StatusCategory.InProgress, null, 1, false, false);
        workflow.AddTransition("Start", todo.Id, inProgress.Id, false, 0);

        workflow.CanTransition(todo.Id, inProgress.Id).Should().BeTrue();
    }

    [Fact]
    public void CanTransition_WithInvalidTransition_ReturnsFalse()
    {
        var workflow = Workflow.Create("Test", null, false, Guid.NewGuid());

        workflow.CanTransition(1, 99).Should().BeFalse();
    }

    [Fact]
    public void GetInitialStatus_WithInitialStatus_ReturnsIt()
    {
        var workflow = Workflow.Create("Test", null, false, Guid.NewGuid());
        workflow.AddStatus("Backlog", null, StatusCategory.ToDo, null, 0, true, false);
        workflow.AddStatus("Done", null, StatusCategory.Done, null, 1, false, true);

        var initial = workflow.GetInitialStatus();

        initial.Should().NotBeNull();
        initial!.Name.Should().Be("Backlog");
    }

    [Fact]
    public void Update_IncrementsVersion()
    {
        var workflow = Workflow.Create("Test", null, false, Guid.NewGuid());
        var versionBefore = workflow.Version;

        workflow.Update("Updated Name", null);

        workflow.Version.Should().Be(versionBefore + 1);
    }
}
