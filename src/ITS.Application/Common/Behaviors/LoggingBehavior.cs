using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ITS.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = currentUserService.IsAuthenticated ? currentUserService.UserId.ToString() : "anonymous";

        logger.LogInformation("ITS Request: {RequestName} by User {UserId}", requestName, userId);

        try
        {
            var response = await next();
            logger.LogInformation("ITS Request: {RequestName} completed successfully", requestName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ITS Request: {RequestName} failed. User: {UserId}", requestName, userId);
            throw;
        }
    }
}
