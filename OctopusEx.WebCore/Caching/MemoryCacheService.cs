namespace OctopusEx.WebCore.Caching;

using Microsoft.Extensions.Caching.Memory;
using Observability;

/// <summary>
/// 基于 IMemoryCache 的进程内缓存（L1）。
/// 自带 SemaphoreSlim 单飞机制，同一 key 并发 GetOrAdd 只执行一次 factory。
/// </summary>
public class MemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ConcurrentDictionary<String, SemaphoreSlim> _locks = new();

    public MemoryCacheService(IMemoryCache cache, CacheOptions options)
    {
        _cache = cache;
        _options = options;
    }

    public Task<T?> GetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);
        var hit = _cache.TryGetValue<T>(fullKey, out var value);
        OctopusTelemetry.CacheHits.Add(1, new KeyValuePair<String, Object?>("layer", hit ? "L1" : "MISS"));
        return Task.FromResult(hit ? value : default);
    }

    public Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);
        var actualTtl = _options.ApplyJitter(ttl ?? _options.DefaultTtl);
        _cache.Set(fullKey, value, actualTtl);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(String key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(_options.BuildKey(key));
        return Task.CompletedTask;
    }

    public Task<Boolean> ExistsAsync(String key, CancellationToken cancellationToken = default)
        => Task.FromResult(_cache.TryGetValue(_options.BuildKey(key), out _));

    public async Task<T?> GetOrAddAsync<T>(
        String key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);

        if (_cache.TryGetValue<CacheEntry<T>>(fullKey, out var entry) && entry != null)
            return entry.Value;

        var semaphore = _locks.GetOrAdd(fullKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // 双检：等待期间可能已被其他请求填充
            if (_cache.TryGetValue<CacheEntry<T>>(fullKey, out entry) && entry != null)
                return entry.Value;

            OctopusTelemetry.CacheFactoryExecutions.Add(1);
            var value = await factory(cancellationToken);

            if (value != null)
            {
                var actualTtl = _options.ApplyJitter(ttl ?? _options.DefaultTtl);
                _cache.Set(fullKey, new CacheEntry<T>(value), actualTtl);
            }
            else if (_options.CacheNullValues)
            {
                _cache.Set(fullKey, new CacheEntry<T>(default!), _options.NullValueTtl);
            }

            return value;
        }
        finally
        {
            semaphore.Release();
            // 简单清理：信号量空闲时移除（避免无限增长）
            if (semaphore.CurrentCount == 1 && _locks.TryRemove(fullKey, out var removed) && removed != semaphore)
                _locks.TryAdd(fullKey, removed);
        }
    }

    public void Dispose()
    {
        foreach (var sem in _locks.Values) sem.Dispose();
        _locks.Clear();
    }

    /// <summary>包装类，区分"未命中"与"命中但值为 null"</summary>
    private sealed record CacheEntry<T>(T Value);
}
