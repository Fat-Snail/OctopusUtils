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

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public DistributedCacheService(IDistributedCache cache, CacheOptions options, ILogger<DistributedCacheService> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(String key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(_options.BuildKey(key), cancellationToken);
        return bytes == null ? default : Deserialize<T>(bytes);
    }

    public Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var actualTtl = _options.ApplyJitter(ttl ?? _options.DefaultTtl);
        var entry = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = actualTtl };
        return _cache.SetAsync(_options.BuildKey(key), Serialize(value), entry, cancellationToken);
    }

    public Task RemoveAsync(String key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(_options.BuildKey(key), cancellationToken);

    public async Task<Boolean> ExistsAsync(String key, CancellationToken cancellationToken = default)
        => (await _cache.GetAsync(_options.BuildKey(key), cancellationToken)) != null;

    public async Task<T?> GetOrAddAsync<T>(
        String key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await GetAsync<T>(key, cancellationToken);
            if (existing != null) return existing;
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
