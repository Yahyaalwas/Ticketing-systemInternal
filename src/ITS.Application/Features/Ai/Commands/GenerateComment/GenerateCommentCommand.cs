using MediatR;

namespace ITS.Application.Features.Ai.Commands.GenerateComment;

public sealed record GenerateCommentCommand(
    Guid TicketId,
    string Instruction,
    CommentTone Tone = CommentTone.Professional
) : IRequest<GeneratedCommentDto>;

public enum CommentTone
{
    Professional,
    Technical,
    CustomerFacing,
    StatusUpdate,
    FollowUp
}

public sealed record GeneratedCommentDto(
    string DraftBody,
    string Tone,
    string ProviderName,
    DateTimeOffset GeneratedAt
);
