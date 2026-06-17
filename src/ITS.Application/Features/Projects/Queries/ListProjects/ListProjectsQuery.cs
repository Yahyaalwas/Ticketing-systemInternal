using ITS.Application.Features.Projects.Queries.GetProject;
using MediatR;

namespace ITS.Application.Features.Projects.Queries.ListProjects;

public sealed record ListProjectsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsArchived = null,
    int? DepartmentId = null,
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<ProjectListResult>;

public sealed record ProjectListResult(
    IReadOnlyList<ProjectSummaryDto> Items,
    int TotalCount,
    int PageNumber,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
