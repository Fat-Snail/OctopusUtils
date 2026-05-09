namespace OctopusEx.WebCore.Extensions;

using Caching;
using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// 缓存注册扩展。
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// 注册简单内存缓存（L1 only）。
    /// </summary>
    public static IServiceCollection AddSimpleCache(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<ICacheService>(sp => sp.GetRequiredService<MemoryCacheService>());
        return services;
    }

    /// <summary>
    /// 注册多级缓存（L1 内存 + L2 分布式）。
    /// 调用方需先注册 IDistributedCache（如 services.AddStackExchangeRedisCache(...)）。
    /// </summary>
    public static IServiceCollection AddMultiLevelCache(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null,
        TimeSpan? l1Ttl = null)
    {
        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheService>();
        services.AddSingleton<DistributedCacheService>();
        services.AddSingleton<ICacheService>(sp => new MultiLevelCacheService(
            sp.GetRequiredService<MemoryCacheService>(),
            sp.GetRequiredService<DistributedCacheService>(),
            sp.GetRequiredService<ILogger<MultiLevelCacheService>>(),
            l1Ttl));
        return services;
    }
}
