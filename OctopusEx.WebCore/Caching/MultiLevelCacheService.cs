namespace OctopusEx.WebCore.Caching;

/// <summary>
/// L1 内存 + L2 分布式的多级缓存。
/// 读取顺序：L1 → L2 → factory。L2 异常时自动降级为仅 L1。
/// 写入：同时写入两级；L2 写失败不影响 L1。
/// 删除：同时删除两级。
/// </summary>
public class MultiLevelCacheService : ICacheService
{
    private readonly MemoryCacheService _l1;
    private readonly DistributedCacheService _l2;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly TimeSpan _l1Ttl;

    public MultiLevelCacheService(
        MemoryCacheService l1,
        DistributedCacheService l2,
        ILogger<MultiLevelCacheService> logger,
        TimeSpan? l1Ttl = null)
    {
        _l1 = l1;
        _l2 = l2;
        _logger = logger;
        _l1Ttl = l1Ttl ?? TimeSpan.FromMinutes(1);
    }

    public async Task<T?> GetAsync<T>(String key, CancellationToken cancellationToken = default)
        => (await TryGetAsync<T>(key, cancellationToken)).Value;

    public async Task<CacheResult<T>> TryGetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        // L1 命中（包括缓存了 null 的穿透防护项）直接返回，不再查 L2
        var l1 = await _l1.TryGetAsync<T>(key, cancellationToken);
        if (l1.Found) return l1;

        try
        {
            var l2 = await _l2.TryGetAsync<T>(key, cancellationToken);
            if (l2.Found)
                await _l1.SetAsync(key, l2.Value!, _l1Ttl, cancellationToken);
            return l2;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 cache unavailable, degrading to L1 only for key {Key}", key);
            return CacheResult<T>.Miss;
        }
    }

    public async Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        await _l1.SetAsync(key, value, _l1Ttl, cancellationToken);
        try
        {
            await _l2.SetAsync(key, value, ttl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 cache set failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(String key, CancellationToken cancellationToken = default)
    {
        await _l1.RemoveAsync(key, cancellationToken);
        try
        {
            await _l2.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "L2 cache remove failed for key {Key}", key);
        }
    }

    public async Task<Boolean> ExistsAsync(String key, CancellationToken cancellationToken = default)
    {
        if (await _l1.ExistsAsync(key, cancellationToken)) return true;
        try { return await _l2.ExistsAsync(key, cancellationToken); }
        catch { return false; }
    }

    public async Task<T?> GetOrAddAsync<T>(
        String key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        // L1 单飞执行：内部 factory 先查 L2，再 fallback 到外部 factory
        return await _l1.GetOrAddAsync(key, async ct =>
        {
            // 用 TryGetAsync 区分 miss 与 cached null：L2 cached null 直接返回，不触发外部 factory（穿透防护）
            try
            {
                var l2 = await _l2.TryGetAsync<T>(key, ct);
                if (l2.Found) return l2.Value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "L2 lookup failed inside GetOrAdd for key {Key}", key);
            }

            var value = await factory(ct);
            if (value != null)
            {
                try { await _l2.SetAsync(key, value, ttl, ct); }
                catch (Exception ex) { _logger.LogWarning(ex, "L2 backfill failed for key {Key}", key); }
            }
            return value;
        }, _l1Ttl, cancellationToken);
    }
}
