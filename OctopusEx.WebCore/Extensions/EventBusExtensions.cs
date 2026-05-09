namespace OctopusEx.WebCore.Extensions;

using Events;

/// <summary>事件总线注册扩展</summary>
public static class EventBusExtensions
{
    /// <summary>
    /// 注册进程内事件总线。
    /// 自动扫描指定程序集，把所有 IEventHandler&lt;T&gt; 实现注册为 Scoped 服务。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="handlerAssemblies">扫描的程序集；为空时使用 EntryAssembly 与 IEventHandler 所在程序集</param>
    /// <param name="configure">事件总线配置回调</param>
    public static IServiceCollection AddSimpleEventBus(
        this IServiceCollection services,
        IEnumerable<Assembly>? handlerAssemblies = null,
        Action<EventBusOptions>? configure = null)
    {
        var options = new EventBusOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddSingleton<IDeadLetterStore>(_ => new InMemoryDeadLetterStore());
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddScoped<IDomainEventCollector, DomainEventCollector>();

        var assemblies = (handlerAssemblies?.ToList())
            ?? new List<Assembly> { Assembly.GetEntryAssembly()!, typeof(IEventHandler<>).Assembly }
                .Where(a => a != null).Distinct().ToList();

        foreach (var asm in assemblies) RegisterHandlersFromAssembly(services, asm);

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
                services.AddScoped(iface, type);
        }
    }
}
