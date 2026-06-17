using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ITS.Infrastructure.Ai;

public sealed class AiRateLimiterOptions
{
    public const string SectionName = "Ai:RateLimit";
    public int RequestsPerMinute { get; set; } = 20;
    public bool Enabled { get; set; } = true;
}

public sealed class AiRateLimiter(IMemoryCache cache, IOptions<AiRateLimiterOptions> options) : IAiRateLimiter
{
    private readonly AiRateLimiterOptions _opts = options.Value;

    public Task<bool> IsAllowedAsync(string userId, string feature, CancellationToken ct = default)
    {
        if (!_opts.Enabled) return Task.FromResult(true);

        var key = $"ai:rate:{userId}:{feature}:{DateTime.UtcNow:yyyyMMddHHmm}";
        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return 0;
        });

        if (count >= _opts.RequestsPerMinute)
            return Task.FromResult(false);

        cache.Set(key, count + 1, TimeSpan.FromMinutes(2));
        return Task.FromResult(true);
    }
}
