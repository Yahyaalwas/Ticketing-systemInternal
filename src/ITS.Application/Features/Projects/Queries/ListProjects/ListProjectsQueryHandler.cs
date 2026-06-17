using ITS.Application.Common.Interfaces;
using ITS.Application.Features.Projects.Queries.GetProject;
using ITS.Domain.Entities.Projects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ITS.Application.Features.Projects.Queries.ListProjects;

public sealed class ListProjectsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IProjectAuthorizationService authz)
    : IRequestHandler<ListProjectsQuery, ProjectListResult>
{
    public async Task<ProjectListResult> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var accessibleIds = await authz.GetAccessibleProjectIdsAsync(currentUser.UserId, cancellationToken);

        var query = db.Projects.AsNoTracking()
            .Where(p => accessibleIds.Contains(p.Id) && !p.IsDeleted);

        if (request.IsArchived.HasValue)
            query = query.Where(p => p.IsArchived == request.IsArchived.Value);

        if (request.DepartmentId.HasValue)
            query = query.Where(p => p.DepartmentId == request.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.ProjectKey.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        IQueryable<Project> sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "key"     => request.SortDescending ? query.OrderByDescending(p => p.ProjectKey) : query.OrderBy(p => p.ProjectKey),
            "created" => request.SortDescending ? query.OrderByDescending(p => p.CreatedAt)  : query.OrderBy(p => p.CreatedAt),
            "updated" => request.SortDescending ? query.OrderByDescending(p => p.UpdatedAt)  : query.OrderBy(p => p.UpdatedAt),
            _         => request.SortDescending ? query.OrderByDescending(p => p.Name)        : query.OrderBy(p => p.Name)
        };

        var totalCount = await sorted.CountAsync(cancellationToken);

        var page = await sorted
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        if (page.Count == 0)
        {
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
            return new ProjectListResult(
                [],
                totalCount,
                request.PageNumber,
                totalPages,
                request.PageNumber > 1,
                request.PageNumber < totalPages);
        }

        var projectIds = page.Select(p => p.Id).ToList();
        var leadIds    = page.Select(p => p.LeadUserId).Distinct().ToList();
        var deptIds    = page.Select(p => p.DepartmentId).Distinct().ToList();

        var leadsTask = db.Users.AsNoTracking()
            .Where(u => leadIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var deptsTask = db.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, cancellationToken);

        var memberCountsTask = db.ProjectMembers.AsNoTracking()
            .Where(m => projectIds.Contains(m.ProjectId))
            .GroupBy(m => m.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, cancellationToken);

        await Task.WhenAll(leadsTask, deptsTask, memberCountsTask);

        var leads        = await leadsTask;
        var depts        = await deptsTask;
        var memberCounts = await memberCountsTask;

        var items = page.Select(p => new ProjectSummaryDto(
            p.Id,
            p.ProjectKey,
            p.Name,
            p.Description,
            p.AvatarUrl,
            p.LeadUserId,
            leads.GetValueOrDefault(p.LeadUserId, ""),
            p.DepartmentId,
            depts.GetValueOrDefault(p.DepartmentId),
            p.IsArchived,
            p.CreatedAt,
            memberCounts.GetValueOrDefault(p.Id)))
            .ToList();

        var pages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new ProjectListResult(
            items.AsReadOnly(),
            totalCount,
            request.PageNumber,
            pages,
            request.PageNumber > 1,
            request.PageNumber < pages);
    }
}
