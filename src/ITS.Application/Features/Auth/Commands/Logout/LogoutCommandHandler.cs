using ITS.Application.Common.Interfaces;
using ITS.Domain.Entities.Audit;
using ITS.Domain.Enums;
using MediatR;

namespace ITS.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(IApplicationDbContext db) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(AuditLog.Create(
            command.UserId, command.IpAddress, command.UserAgent,
            AuditOperation.Logout, "User", command.UserId.ToString(),
            null, null, null, true, null, command.TraceId));

        await db.SaveChangesAsync(cancellationToken);
    }
}
