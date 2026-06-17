using MediatR;

namespace ITS.Application.Features.Activity.Queries.GetTicketActivity;

public sealed record GetTicketActivityQuery(
    Guid TicketId,
    int PageNumber = 1,
    int PageSize = 50)
    : IRequest<TicketActivityResult>;

public sealed record TicketActivityResult(
    IReadOnlyList<ActivityItemDto> Items,
    int TotalCount,
    int PageNumber,
    int TotalPages);

public sealed record ActivityItemDto(
    long Id,
    string ActivityType,
    string EntityType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    Guid ActorUserId,
    string ActorName,
    string? ActorAvatarUrl,
    DateTime OccurredAt);
