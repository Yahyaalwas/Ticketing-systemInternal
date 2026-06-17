using ITS.Application.Features.Activity.Queries.GetTicketActivity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
[Produces("application/json")]
public class ActivityController(ISender mediator) : ControllerBase
{
    /// <summary>Get chronological activity timeline for a ticket.</summary>
    [HttpGet("tickets/{ticketId:guid}/activity")]
    [ProducesResponseType(typeof(TicketActivityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TicketActivityResult>> GetTicketActivity(
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetTicketActivityQuery(ticketId, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
