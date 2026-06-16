using ITS.Application.Common.Interfaces;
using ITS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController(
    ApplicationDbContext db,
    IProjectAuthorizationService authz,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> GetProjects(CancellationToken cancellationToken)
    {
        var accessibleIds = await authz.GetAccessibleProjectIdsAsync(currentUser.UserId, cancellationToken);

        var projects = await db.Projects.AsNoTracking()
            .Where(p => accessibleIds.Contains(p.Id))
            .Select(p => new ProjectSummaryDto(
                p.Id, p.ProjectKey, p.Name, p.Description, p.AvatarUrl,
                p.LeadUserId, p.IsArchived, p.CreatedAt))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }

    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectDetailDto>> GetProject(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await authz.CanViewProjectAsync(currentUser.UserId, projectId, cancellationToken))
            return Forbid();

        var project = await db.Projects.AsNoTracking()
            .Include(p => p.Members)
            .Include(p => p.IssueTypes)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project is null) return NotFound();

        var lead = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == project.LeadUserId, cancellationToken);

        return Ok(new ProjectDetailDto(
            project.Id, project.ProjectKey, project.Name, project.Description,
            project.AvatarUrl, project.LeadUserId, lead?.DisplayName ?? "",
            project.DepartmentId, project.ActiveWorkflowId, project.IsArchived,
            project.CreatedAt, project.UpdatedAt, project.RowVersion,
            project.IssueTypes.Select(it => it.Name).ToList(),
            project.Members.Count));
    }
}

public sealed record ProjectSummaryDto(
    Guid Id, string ProjectKey, string Name, string? Description,
    string? AvatarUrl, Guid LeadUserId, bool IsArchived, DateTime CreatedAt);

public sealed record ProjectDetailDto(
    Guid Id, string ProjectKey, string Name, string? Description,
    string? AvatarUrl, Guid LeadUserId, string LeadDisplayName,
    int DepartmentId, Guid? ActiveWorkflowId, bool IsArchived,
    DateTime CreatedAt, DateTime UpdatedAt, byte[] RowVersion,
    IReadOnlyList<string> IssueTypes, int MemberCount);
