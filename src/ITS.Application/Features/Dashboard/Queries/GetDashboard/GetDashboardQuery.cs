using MediatR;

namespace ITS.Application.Features.Dashboard.Queries.GetDashboard;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed record DashboardDto(
    IReadOnlyList<DashboardTicketDto> MyOpenTickets,
    IReadOnlyList<DashboardTicketDto> AssignedToMe,
    IReadOnlyList<DashboardTicketDto> ReportedByMe,
    IReadOnlyList<DashboardTicketDto> OverdueTickets,
    IReadOnlyList<DashboardTicketDto> RecentlyUpdated,
    IReadOnlyList<PriorityBreakdownDto> TicketsByPriority,
    IReadOnlyList<StatusBreakdownDto> TicketsByStatus,
    IReadOnlyList<ProjectBreakdownDto> TicketsByProject);

public sealed record DashboardTicketDto(
    Guid TicketId,
    string TicketKey,
    string Title,
    string ProjectName,
    string StatusName,
    string? PriorityName,
    string? PriorityColor,
    Guid? AssigneeUserId,
    string? AssigneeName,
    DateOnly? DueDate,
    DateTime? SlaBreachAt,
    bool IsSlaBreached,
    DateTime UpdatedAt);

public sealed record PriorityBreakdownDto(int? PriorityId, string PriorityName, int Count);
public sealed record StatusBreakdownDto(int StatusId, string StatusName, string Category, int Count);
public sealed record ProjectBreakdownDto(Guid ProjectId, string ProjectName, int Count);
