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
        // 通过 CacheEntry<T> 包装区分 miss 与 cached null
        var hit = _cache.TryGetValue<CacheEntry<T>>(fullKey, out var entry) && entry != null;
        OctopusTelemetry.CacheHits.Add(1, new KeyValuePair<String, Object?>("layer", hit ? "L1" : "MISS"));
        return Task.FromResult(hit ? entry!.Value : default);
    }

    public Task<CacheResult<T>> TryGetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);
        if (_cache.TryGetValue<CacheEntry<T>>(fullKey, out var entry) && entry != null)
        {
            OctopusTelemetry.CacheHits.Add(1, new KeyValuePair<String, Object?>("layer", "L1"));
            return Task.FromResult(CacheResult<T>.Hit(entry.Value));
        }
        OctopusTelemetry.CacheHits.Add(1, new KeyValuePair<String, Object?>("layer", "MISS"));
        return Task.FromResult(CacheResult<T>.Miss);
    }

    public Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);
        var actualTtl = _options.ApplyJitter(ttl ?? _options.DefaultTtl);
        // 统一存储为 CacheEntry<T>，让 GetAsync / TryGetAsync 能区分 miss 与 cached null
        _cache.Set(fullKey, new CacheEntry<T>(value), actualTtl);
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
            // 注意：故意不清理 _locks 字典。
            // 之前版本的 TryRemove 清理逻辑存在竞态：清理后另一并发请求会创建新信号量，
            // 双方拿的不是同一把锁，单飞机制失效；同时已等待者可能拿到 disposed semaphore。
            // 字典随 unique key 数量增长，但典型场景（key 数量有界）可接受。
            // 如需严格 GC，请用 LRU 缓存包裹本服务。
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var sem in _locks.Values) sem.Dispose();
        _locks.Clear();
    }

    private Boolean _disposed;

    /// <summary>包装类，区分"未命中"与"命中但值为 null"</summary>
    private sealed record CacheEntry<T>(T Value);
}
