using MediatR;

namespace ITS.Application.Features.Projects.Queries.GetProject;

public sealed record GetProjectQuery(Guid ProjectId) : IRequest<ProjectDetailDto>;

public sealed record ProjectDetailDto(
    Guid Id,
    string ProjectKey,
    string Name,
    string? Description,
    string? AvatarUrl,
    Guid LeadUserId,
    string LeadDisplayName,
    int DepartmentId,
    string? DepartmentName,
    Guid? ActiveWorkflowId,
    string? ActiveWorkflowName,
    bool IsArchived,
    DateTime? ArchivedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    byte[] RowVersion,
    IReadOnlyList<string> IssueTypes,
    IReadOnlyList<ProjectMemberDto> Members,
    int MemberCount,
    int ActiveTicketCount);

public sealed record ProjectMemberDto(
    Guid MemberId,
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    string RoleName,
    DateTime GrantedAt);

public sealed record ProjectSummaryDto(
    Guid Id,
    string ProjectKey,
    string Name,
    string? Description,
    string? AvatarUrl,
    Guid LeadUserId,
    string LeadDisplayName,
    int DepartmentId,
    string? DepartmentName,
    bool IsArchived,
    DateTime CreatedAt,
    int MemberCount);
