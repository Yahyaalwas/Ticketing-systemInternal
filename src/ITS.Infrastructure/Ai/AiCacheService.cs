using ITS.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ITS.Infrastructure.Ai;

public sealed class AiCacheService(IMemoryCache cache) : IAiCacheService
{
    private readonly HashSet<string> _keys = [];
    private readonly object _lock = new();

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out string? value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken ct = default)
    {
        cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        lock (_lock) { _keys.Add(key); }
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
        return Task.CompletedTask;
    }

    public Task InvalidateByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        string[] matching;
        lock (_lock) { matching = [.. _keys.Where(k => k.StartsWith(prefix))]; }
        foreach (var key in matching)
        {
            cache.Remove(key);
            lock (_lock) { _keys.Remove(key); }
        }
        return Task.CompletedTask;
    }
}
