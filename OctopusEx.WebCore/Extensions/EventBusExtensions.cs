namespace OctopusEx.WebCore.Extensions;

using Events;
using Events.Outbox;
using Idempotency;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>事件总线注册扩展</summary>
public static class EventBusExtensions
{
    /// <summary>
    /// 注册进程内事件总线。
    /// 自动扫描指定程序集，把所有 IEventHandler&lt;T&gt; 实现注册为 Scoped 服务（去重）。
    /// </summary>
    public static IServiceCollection AddSimpleEventBus(
        this IServiceCollection services,
        IEnumerable<Assembly>? handlerAssemblies = null,
        Action<EventBusOptions>? configure = null)
    {
        var options = new EventBusOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.TryAddSingleton<IDeadLetterStore>(_ => new InMemoryDeadLetterStore());
        services.TryAddSingleton<IEventBus, InMemoryEventBus>();
        services.TryAddScoped<IDomainEventCollector, DomainEventCollector>();

        var assemblies = (handlerAssemblies?.ToList())
            ?? new List<Assembly> { Assembly.GetEntryAssembly()!, typeof(IEventHandler<>).Assembly }
                .Where(a => a != null).Distinct().ToList();

        foreach (var asm in assemblies) RegisterHandlersFromAssembly(services, asm);

        return services;
    }

    /// <summary>
    /// 注册 Outbox（内存存储）。生产环境请使用 <see cref="AddEFOutbox{TContext}"/>。
    /// </summary>
    public static IServiceCollection AddOutbox(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null,
        Boolean enableNotifier = true)
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        if (enableNotifier)
            services.TryAddSingleton<IOutboxNotifier, ChannelOutboxNotifier>();

        services.TryAddSingleton<InMemoryOutboxStore>();
        services.TryAddSingleton<IOutboxStore>(sp => sp.GetRequiredService<InMemoryOutboxStore>());

        services.AddHostedService<OutboxDispatcher>();
        return services;
    }

    /// <summary>
    /// 注册 EF Core Outbox 存储。需在 DbContext.OnModelCreating 中调用 modelBuilder.AddOctopusOutbox()。
    /// </summary>
    /// <typeparam name="TContext">业务 DbContext 类型</typeparam>
    public static IServiceCollection AddEFOutbox<TContext>(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null,
        Boolean enableNotifier = true)
        where TContext : DbContext
    {
        var options = new OutboxOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        if (enableNotifier)
            services.TryAddSingleton<IOutboxNotifier, ChannelOutboxNotifier>();

        services.AddScoped<IOutboxStore>(sp => new EFOutboxStore(
            sp.GetRequiredService<TContext>(),
            sp.GetService<ILogger<EFOutboxStore>>()));

        services.AddHostedService<OutboxDispatcher>();
        return services;
    }

    /// <summary>
    /// 注册幂等存储（EF Core 实现）。需在 DbContext.OnModelCreating 中调用 modelBuilder.AddOctopusIdempotency()。
    /// 自动注册 HTTP 幂等中间件和过期清理后台服务。
    /// </summary>
    /// <typeparam name="TContext">业务 DbContext 类型</typeparam>
    public static IServiceCollection AddEFIdempotency<TContext>(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configure = null)
        where TContext : DbContext
    {
        var options = new IdempotencyOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddScoped<IIdempotencyStore>(sp => new EFIdempotencyStore(
            sp.GetRequiredService<TContext>(),
            sp.GetRequiredService<IdempotencyOptions>()));

        services.AddHostedService<IdempotencyCleanupBackgroundService>();
        return services;
    }

    /// <summary>
    /// 注册幂等存储（Redis 实现）。需先注册 <see cref="IRedisIdempotencyConnection"/>。
    /// </summary>
    public static IServiceCollection AddRedisIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions>? configure = null,
        String keyPrefix = "octopus:idempotency:")
    {
        var options = new IdempotencyOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddSingleton<IIdempotencyStore>(sp => new RedisIdempotencyStore(
            sp.GetRequiredService<IRedisIdempotencyConnection>(),
            sp.GetRequiredService<IdempotencyOptions>(),
            keyPrefix));

        // Redis 通过 TTL 自动清理，不需要后台服务
        return services;
    }

    /// <summary>
    /// 注册 RedisEventBus。
    /// 调用方需先注册 <see cref="IRedisEventBusConnection"/>（Redis 连接抽象）。
    /// 同时保留 InMemoryEventBus 用于本进程派发。
    /// </summary>
    public static IServiceCollection AddRedisEventBus(
        this IServiceCollection services,
        String channelPrefix = "octopus:events:")
    {
        // 保证 InMemory 已注册（Redis bus 收到消息后用其分发本进程 handlers）
        services.TryAddSingleton<InMemoryEventBus>();
        services.TryAddSingleton<IDeadLetterStore>(_ => new InMemoryDeadLetterStore());
        services.TryAddSingleton(new EventBusOptions());

        services.AddSingleton<RedisEventBus>(sp => new RedisEventBus(
            sp.GetRequiredService<IRedisEventBusConnection>(),
            sp.GetRequiredService<InMemoryEventBus>(),
            sp.GetRequiredService<ILogger<RedisEventBus>>(),
            channelPrefix));

        // 默认把 IEventBus 替换为 Redis（跨进程发布），用户也可显式 services.AddSingleton<IEventBus, InMemoryEventBus>() 退回本地
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RedisEventBus>());
        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface) continue;

            var handlerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            foreach (var iface in handlerInterfaces)
            {
                // 去重：同一 (iface, impl) 已注册则跳过
                if (services.Any(s => s.ServiceType == iface && s.ImplementationType == type)) continue;
                services.AddScoped(iface, type);
            }
        }
    }
}
