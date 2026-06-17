using MediatR;
namespace ITS.Application.Features.Reference.GetRoles;

public sealed record GetRolesQuery(string? Scope = null) : IRequest<IReadOnlyList<RoleRefDto>>;
public sealed record RoleRefDto(int Id, string Name, string Scope, string? Description);
