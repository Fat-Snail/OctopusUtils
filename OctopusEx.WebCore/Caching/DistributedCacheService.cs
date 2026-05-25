namespace OctopusEx.WebCore.Caching;

using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// 基于 IDistributedCache 的分布式缓存（L2）。
/// 用户配置 Redis / SQL Server 等具体实现，本类不绑定 Redis 客户端。
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<DistributedCacheService> _logger;
    private readonly IDistributedCacheKeyChecker? _keyChecker;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public DistributedCacheService(
        IDistributedCache cache,
        CacheOptions options,
        ILogger<DistributedCacheService> logger,
        IDistributedCacheKeyChecker? keyChecker = null)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
        _keyChecker = keyChecker;
    }

    public async Task<T?> GetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(_options.BuildKey(key), cancellationToken);
        return bytes == null ? default : Deserialize<T>(bytes);
    }

    public async Task<CacheResult<T>> TryGetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        // L2 layer: bytes == null → miss; bytes != null → hit (Deserialize 可能返回 null = cached null)
        var bytes = await _cache.GetAsync(_options.BuildKey(key), cancellationToken);
        return bytes == null ? CacheResult<T>.Miss : CacheResult<T>.Hit(Deserialize<T>(bytes));
    }

    public Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var actualTtl = _options.ApplyJitter(ttl ?? _options.DefaultTtl);
        var entry = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = actualTtl };
        return _cache.SetAsync(_options.BuildKey(key), Serialize(value), entry, cancellationToken);
    }

    public Task RemoveAsync(String key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(_options.BuildKey(key), cancellationToken);

    public Task<Boolean> ExistsAsync(String key, CancellationToken cancellationToken = default)
    {
        var fullKey = _options.BuildKey(key);
        // 优先用注入的 key checker（典型 Redis 场景调 EXISTS 命令，零 payload 传输）
        if (_keyChecker != null)
            return _keyChecker.ExistsAsync(fullKey, cancellationToken);
        // Fallback：拉 payload 后判 null。大值场景浪费带宽，建议注册 IDistributedCacheKeyChecker
        return Fallback();

        async Task<Boolean> Fallback() => (await _cache.GetAsync(fullKey, cancellationToken)) != null;
    }

    public async Task<T?> GetOrAddAsync<T>(
        String key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        // 用 TryGetAsync 区分 miss 与 cached null：cached null 直接返回，不再触发 factory（穿透防护）
        try
        {
            var existing = await TryGetAsync<T>(key, cancellationToken);
            if (existing.Found) return existing.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed cache GET failed for key {Key}, falling through to factory", key);
        }

        var value = await factory(cancellationToken);
        try
        {
            if (value != null)
                await SetAsync(key, value, ttl, cancellationToken);
            else if (_options.CacheNullValues)
                await SetAsync<T?>(key, default, _options.NullValueTtl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Distributed cache SET failed for key {Key}, value not cached", key);
        }
        return value;
    }

    private static Byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

    private static T? Deserialize<T>(Byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
}
