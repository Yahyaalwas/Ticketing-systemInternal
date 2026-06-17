using MediatR;
namespace ITS.Application.Features.Reference.SearchUsers;

public sealed record SearchUsersQuery(string? Search = null, int Limit = 20) : IRequest<IReadOnlyList<UserRefDto>>;
public sealed record UserRefDto(Guid Id, string DisplayName, string Email, string? AvatarUrl, bool IsActive);
