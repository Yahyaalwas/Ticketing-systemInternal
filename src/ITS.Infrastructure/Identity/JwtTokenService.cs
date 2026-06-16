using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ITS.Infrastructure.Identity;

public sealed class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public string IssueToken(TokenRequest request)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]
                ?? throw new InvalidOperationException("Jwt:Key is required.")));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new(ClaimTypes.Upn, request.UserPrincipalName),
            new(ClaimTypes.Name, request.DisplayName),
            new(ClaimTypes.Email, request.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        claims.AddRange(request.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiry = DateTime.UtcNow.AddHours(
            configuration.GetValue<int>("Jwt:ExpiryHours", 8));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
