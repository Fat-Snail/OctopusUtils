namespace OctopusEx.WebCore.Caching;

/// <summary>
/// 缓存读取结果。Found 区分"未命中"与"命中但值为 null"（用于穿透防护）。
/// </summary>
public readonly record struct CacheResult<T>(Boolean Found, T? Value)
{
    public static CacheResult<T> Miss => default;
    public static CacheResult<T> Hit(T? value) => new(true, value);
}

/// <summary>
/// 统一缓存抽象接口。底层可为内存（L1）、分布式（L2）或多级组合。
/// </summary>
public interface ICacheService
{
    /// <summary>读取缓存值。命中返回值；未命中返回默认值。无法区分"未命中"与"命中 null"，需要时用 TryGetAsync。</summary>
    Task<T?> GetAsync<T>(String key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取缓存值，返回 (Found, Value) 元组。Found = false 表示未命中；
    /// Found = true + Value = null 表示命中且缓存值为 null（穿透防护）。
    /// </summary>
    Task<CacheResult<T>> TryGetAsync<T>(String key, CancellationToken cancellationToken = default);

    /// <summary>写入缓存值。ttl 为 null 时使用 CacheOptions.DefaultTtl。</summary>
    Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>删除缓存。</summary>
    Task RemoveAsync(String key, CancellationToken cancellationToken = default);

    /// <summary>判断键是否存在。</summary>
    Task<Boolean> ExistsAsync(String key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取或从 factory 加载。同一 key 的并发请求只会执行一次 factory（单飞模式，缓存击穿防护）。
    /// 当配置了空值缓存时，factory 返回 null 也会被短期缓存（缓存穿透防护）。
    /// </summary>
    Task<T?> GetOrAddAsync<T>(
        String key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);
}
