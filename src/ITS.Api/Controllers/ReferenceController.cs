using ITS.Application.Features.Reference.GetIssueTypes;
using ITS.Application.Features.Reference.GetPriorities;
using ITS.Application.Features.Reference.GetRoles;
using ITS.Application.Features.Reference.SearchUsers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/reference")]
[Authorize]
[Produces("application/json")]
public class ReferenceController(ISender mediator) : ControllerBase
{
    [HttpGet("priorities")]
    public async Task<ActionResult<IReadOnlyList<PriorityDto>>> GetPriorities(CancellationToken ct = default)
        => Ok(await mediator.Send(new GetPrioritiesQuery(), ct));

    [HttpGet("issue-types")]
    public async Task<ActionResult<IReadOnlyList<IssueTypeDto>>> GetIssueTypes(
        [FromQuery] string? projectId = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetIssueTypesQuery(projectId), ct));

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserRefDto>>> SearchUsers(
        [FromQuery] string? search = null, [FromQuery] int limit = 20, CancellationToken ct = default)
        => Ok(await mediator.Send(new SearchUsersQuery(search, limit), ct));

    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RoleRefDto>>> GetRoles(
        [FromQuery] string? scope = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetRolesQuery(scope), ct));
}
