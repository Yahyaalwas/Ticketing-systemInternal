using System.Diagnostics;
using ITS.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ITS.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var response = await next();
        timer.Stop();

        var elapsed = timer.ElapsedMilliseconds;
        if (elapsed > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            var userId = currentUserService.IsAuthenticated ? currentUserService.UserId.ToString() : "anonymous";

            logger.LogWarning(
                "ITS Slow Request: {RequestName} took {Elapsed}ms. User: {UserId}",
                requestName, elapsed, userId);
        }

        return response;
    }
}
