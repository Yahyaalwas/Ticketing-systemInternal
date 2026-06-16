using ITS.Application.Common.Interfaces;
using ITS.Domain.Enums;
using ITS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ITS.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(
    IAdSyncService adSyncService,
    ApplicationDbContext db,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Authenticate against Active Directory
        var authenticated = await adSyncService.AuthenticateAsync(
            request.UserPrincipalName, request.Password, cancellationToken);

        if (!authenticated)
        {
            logger.LogWarning("Failed login attempt for {UPN} from {IP}",
                request.UserPrincipalName, HttpContext.Connection.RemoteIpAddress);

            // Audit failed login
            db.AuditLogs.Add(Domain.Entities.Audit.AuditLog.Create(
                null, HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                AuditOperation.LoginFailed, "User", request.UserPrincipalName,
                null, null, null, false, "Invalid credentials", HttpContext.TraceIdentifier));
            await db.SaveChangesAsync(cancellationToken);

            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Provision or update user from AD on login
        var adUser = await adSyncService.QueryUserAsync(request.UserPrincipalName, cancellationToken);
        if (adUser is null)
            return Unauthorized(new { message = "User account not found in directory." });

        var user = await db.Users
            .Include(u => u.GlobalRoles)
            .ThenInclude(gr => gr.Role)
            .FirstOrDefaultAsync(u => u.AdObjectId == adUser.ObjectId, cancellationToken);

        if (user is null)
        {
            // First login — provision user
            user = Domain.Entities.Identity.User.ProvisionFromAd(
                adUser.ObjectId, adUser.UserPrincipalName, adUser.Email,
                adUser.DisplayName, adUser.FirstName, adUser.LastName,
                null, adUser.JobTitle, adUser.PhoneNumber);
            db.Users.Add(user);
        }

        user.RecordLogin(DateTime.UtcNow);

        // Issue JWT
        var token = IssueJwt(user);

        // Audit successful login
        db.AuditLogs.Add(Domain.Entities.Audit.AuditLog.Create(
            user.Id, HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            AuditOperation.Login, "User", user.Id.ToString(),
            null, null, null, true, null, HttpContext.TraceIdentifier));

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new LoginResponse(
            token,
            user.Id,
            user.DisplayName,
            user.Email,
            user.AvatarUrl,
            user.GlobalRoles.Select(r => r.Role.Name).ToList()));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            db.AuditLogs.Add(Domain.Entities.Audit.AuditLog.Create(
                userId, HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                AuditOperation.Logout, "User", userId.ToString(),
                null, null, null, true, null, HttpContext.TraceIdentifier));
            await db.SaveChangesAsync(cancellationToken);
        }

        // JWT is stateless — client must discard the token
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await db.Users
            .Include(u => u.GlobalRoles)
            .ThenInclude(gr => gr.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

        if (user is null)
            return Unauthorized(new { message = "User account is inactive or does not exist." });

        return Ok(new MeResponse(
            user.Id, user.UserPrincipalName, user.DisplayName,
            user.Email, user.AvatarUrl, user.TimeZoneId, user.Locale,
            user.GlobalRoles.Select(r => r.Role.Name).ToList()));
    }

    private string IssueJwt(Domain.Entities.Identity.User user)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Upn, user.UserPrincipalName),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(user.GlobalRoles.Select(r => new Claim(ClaimTypes.Role, r.Role.Name)));

        var expiry = DateTime.UtcNow.AddHours(
            configuration.GetValue<int>("Jwt:ExpiryHours", 8));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed record LoginRequest(string UserPrincipalName, string Password);
public sealed record LoginResponse(string Token, Guid UserId, string DisplayName, string Email, string? AvatarUrl, IReadOnlyList<string> Roles);
public sealed record MeResponse(Guid UserId, string Upn, string DisplayName, string Email, string? AvatarUrl, string TimeZoneId, string Locale, IReadOnlyList<string> Roles);
