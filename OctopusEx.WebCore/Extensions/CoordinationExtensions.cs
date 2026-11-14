namespace OctopusEx.WebCore.Extensions;

using Coordination;

/// <summary>分布式协调服务注册扩展。</summary>
public static class CoordinationExtensions
{
    /// <summary>注册 Redis 分布式锁。调用方需先注册 <see cref="IRedisDistributedLockConnection"/>。</summary>
    public static IServiceCollection AddRedisDistributedLock(this IServiceCollection services, String? keyPrefix = null)
    {
        services.AddSingleton<IDistributedLockProvider>(sp =>
            new RedisDistributedLockProvider(sp.GetRequiredService<IRedisDistributedLockConnection>(), keyPrefix));
        return services;
    }

    /// <summary>注册进程内锁，适用于单实例和测试。</summary>
    public static IServiceCollection AddInMemoryDistributedLock(
        this IServiceCollection services,
        String? keyPrefix = null)
    {
        services.AddSingleton<IDistributedLockProvider>(_ => new InMemoryDistributedLockProvider(keyPrefix));
        return services;
    }
}
