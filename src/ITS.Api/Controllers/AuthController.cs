using ITS.Application.Features.Auth.Commands.Login;
using ITS.Application.Features.Auth.Commands.Logout;
using ITS.Application.Features.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await sender.Send(
                new LoginCommand(request.UserPrincipalName, request.Password),
                cancellationToken);

            return Ok(new LoginResponse(
                result.Token, result.UserId, result.DisplayName,
                result.Email, result.AvatarUrl, result.Roles));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await sender.Send(new LogoutCommand(
                userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                HttpContext.TraceIdentifier), cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await sender.Send(new GetCurrentUserQuery(userId), cancellationToken);
        if (user is null)
            return Unauthorized(new { message = "User account is inactive or does not exist." });

        return Ok(new MeResponse(
            user.UserId, user.Upn, user.DisplayName, user.Email,
            user.EmployeeId, user.AvatarUrl, user.TimeZoneId, user.Locale, user.Roles));
    }
}

public sealed record LoginRequest(string UserPrincipalName, string Password);
public sealed record LoginResponse(string Token, Guid UserId, string DisplayName, string Email, string? AvatarUrl, IReadOnlyList<string> Roles);
public sealed record MeResponse(Guid UserId, string Upn, string DisplayName, string Email, string? EmployeeId, string? AvatarUrl, string TimeZoneId, string Locale, IReadOnlyList<string> Roles);
