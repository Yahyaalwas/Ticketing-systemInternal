using MediatR;

namespace ITS.Application.Features.Ai.Queries.AnswerKnowledgeQuestion;

public sealed record AnswerKnowledgeQuestionQuery(
    string Question,
    Guid? ProjectId = null
) : IRequest<KnowledgeAnswerDto>;

public sealed record KnowledgeAnswerDto(
    string Answer,
    IReadOnlyList<CitedTicket> Citations,
    string ProviderName,
    DateTimeOffset AnsweredAt
);

public sealed record CitedTicket(Guid Id, string TicketKey, string Title, string StatusName, string Relevance);
