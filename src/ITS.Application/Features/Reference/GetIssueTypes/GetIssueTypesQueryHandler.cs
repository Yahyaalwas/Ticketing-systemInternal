using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace ITS.Application.Features.Reference.GetIssueTypes;

public sealed class GetIssueTypesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetIssueTypesQuery, IReadOnlyList<IssueTypeDto>>
{
    public async Task<IReadOnlyList<IssueTypeDto>> Handle(GetIssueTypesQuery request, CancellationToken ct)
    {
        var query = db.IssueTypes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.ProjectId) && Guid.TryParse(request.ProjectId, out var pid))
            query = query.Where(t => t.ProjectId == pid);
        return await query
            .OrderBy(t => t.Name)
            .Select(t => new IssueTypeDto(t.Id, t.Name, t.IconUrl, t.ProjectId.ToString(), t.IsSubTask, t.IsEpic))
            .ToListAsync(ct);
    }
}
