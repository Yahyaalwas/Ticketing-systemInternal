namespace ITS.Application.Common.Interfaces;

public interface IAiCacheService
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
    Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default);
}
