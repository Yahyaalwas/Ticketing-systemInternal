namespace ITS.Application.Common.Interfaces;

public interface IAiRateLimiter
{
    /// <summary>Returns true if the request is allowed; false if rate limit is exceeded.</summary>
    Task<bool> IsAllowedAsync(string userId, string feature, CancellationToken ct = default);
}
