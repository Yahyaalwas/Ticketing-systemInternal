using ITS.Application.Features.Projects.Commands.AddMember;
using ITS.Application.Features.Projects.Commands.ArchiveProject;
using ITS.Application.Features.Projects.Commands.AssignProjectLead;
using ITS.Application.Features.Projects.Commands.ChangeMemberRole;
using ITS.Application.Features.Projects.Commands.CreateProject;
using ITS.Application.Features.Projects.Commands.DeleteProject;
using ITS.Application.Features.Projects.Commands.RemoveMember;
using ITS.Application.Features.Projects.Commands.UpdateProject;
using ITS.Application.Features.Projects.Queries.GetProject;
using ITS.Application.Features.Projects.Queries.ListProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController(ISender mediator) : ControllerBase
{
    /// <summary>List projects with optional pagination, search, and filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> ListProjects(
        [FromQuery] string? search,
        [FromQuery] int? departmentId,
        [FromQuery] bool? isArchived,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListProjectsQuery(search, departmentId, isArchived, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Get detailed project information including members and issue types.</summary>
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(
        Guid projectId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(projectId), cancellationToken);

        var eTag = Convert.ToBase64String(result.RowVersion);
        Response.Headers.ETag = $"\"{eTag}\"";

        return Ok(result);
    }

    /// <summary>Create a new project.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateProjectResponse>> CreateProject(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand(
            request.ProjectKey, request.Name, request.Description,
            request.LeadUserId, request.DepartmentId, request.WorkflowId);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProject), new { projectId = result.ProjectId }, result);
    }

    /// <summary>Update an existing project. Requires If-Match header for optimistic concurrency.</summary>
    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProject(
        Guid projectId,
        [FromBody] UpdateProjectRequest request,
        [FromHeader(Name = "If-Match")] string eTag,
        CancellationToken cancellationToken)
    {
        var rowVersion = Convert.FromBase64String(eTag.Trim('"'));
        var command = new UpdateProjectCommand(
            projectId, request.Name, request.Description,
            request.LeadUserId, request.AvatarUrl, request.WorkflowId, rowVersion);
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Archive a project.</summary>
    [HttpPost("{projectId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ArchiveProject(
        Guid projectId, CancellationToken cancellationToken)
    {
        await mediator.Send(new ArchiveProjectCommand(projectId), cancellationToken);
        return NoContent();
    }

    /// <summary>Soft-delete a project.</summary>
    [HttpDelete("{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProject(
        Guid projectId, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProjectCommand(projectId), cancellationToken);
        return NoContent();
    }

    /// <summary>Add a member to a project.</summary>
    [HttpPost("{projectId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddMember(
        Guid projectId,
        [FromBody] AddMemberRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new AddMemberCommand(projectId, request.UserId, request.RoleId), cancellationToken);
        return NoContent();
    }

    /// <summary>Remove a member from a project.</summary>
    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveMember(
        Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveMemberCommand(projectId, userId), cancellationToken);
        return NoContent();
    }

    /// <summary>Change a project member's role.</summary>
    [HttpPatch("{projectId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeMemberRole(
        Guid projectId,
        Guid userId,
        [FromBody] ChangeMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new ChangeMemberRoleCommand(projectId, userId, request.RoleId), cancellationToken);
        return NoContent();
    }

    /// <summary>Assign a new project lead.</summary>
    [HttpPut("{projectId:guid}/lead")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignProjectLead(
        Guid projectId,
        [FromBody] AssignProjectLeadRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignProjectLeadCommand(projectId, request.NewLeadUserId), cancellationToken);
        return NoContent();
    }
}

public sealed record CreateProjectRequest(
    string ProjectKey,
    string Name,
    string? Description,
    Guid LeadUserId,
    int DepartmentId,
    Guid? WorkflowId);

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    Guid LeadUserId,
    string? AvatarUrl,
    Guid? WorkflowId);

public sealed record AddMemberRequest(Guid UserId, int RoleId);

public sealed record ChangeMemberRoleRequest(int RoleId);

public sealed record AssignProjectLeadRequest(Guid NewLeadUserId);
