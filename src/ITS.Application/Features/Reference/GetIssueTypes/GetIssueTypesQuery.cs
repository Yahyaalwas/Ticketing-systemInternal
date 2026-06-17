using MediatR;
namespace ITS.Application.Features.Reference.GetIssueTypes;

public sealed record GetIssueTypesQuery(string? ProjectId = null) : IRequest<IReadOnlyList<IssueTypeDto>>;
public sealed record IssueTypeDto(int Id, string Name, string? IconUrl, string ProjectId, bool IsSubTask, bool IsEpic);
