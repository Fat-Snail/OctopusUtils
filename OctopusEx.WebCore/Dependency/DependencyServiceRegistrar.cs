using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Util.Dependency;
using Util.Reflections;

namespace Util.Infrastructure;

/// <summary>
/// 依赖服务注册器 - 用于扫描注册ISingletonDependency,IScopeDependency,ITransientDependency
/// </summary>
public class DependencyServiceRegistrar : IServiceRegistrar
{
    /// <summary>
    /// 获取服务名
    /// </summary>
    public static String ServiceName => "Util.Infrastructure.DependencyServiceRegistrar";

    /// <summary>
    /// 排序号
    /// </summary>
    public Int32 OrderId => 100;

    /// <summary>
    /// 是否启用
    /// </summary>
    public Boolean Enabled => ServiceRegistrarConfig.IsEnabled(ServiceName);

    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="serviceContext">服务上下文</param>
    public Action Register(ServiceContext serviceContext)
    {
        return () =>
        {
            Console.WriteLine($"[DependencyServiceRegistrar] 开始注册依赖服务 (Enabled={Enabled})");
            serviceContext.HostBuilder.ConfigureServices((context, services) =>
            {
                RegisterDependency<ISingletonDependency>(services, serviceContext.TypeFinder,
                    ServiceLifetime.Singleton);
                RegisterDependency<IScopeDependency>(services, serviceContext.TypeFinder, ServiceLifetime.Scoped);
                RegisterDependency<ITransientDependency>(services, serviceContext.TypeFinder,
                    ServiceLifetime.Transient);
                Console.WriteLine($"[DependencyServiceRegistrar] 依赖服务注册完成");
            });
        };
    }

    /// <summary>
    /// 注册依赖
    /// </summary>
    private void RegisterDependency<TDependencyInterface>(IServiceCollection services, ITypeFinder finder,
        ServiceLifetime lifetime)
    {
        var types = GetTypes<TDependencyInterface>(finder);
        var result = FilterTypes(types);
        foreach ( var item in result )
            RegisterType(services, item.Item1, item.Item2, lifetime);
    }

    /// <summary>
    /// 获取接口类型和实现类型列表
    /// </summary>
    private List<(Type, Type)> GetTypes<TDependencyInterface>(ITypeFinder finder)
    {
        var result = new List<(Type, Type)>();
        var classTypes = finder.Find<TDependencyInterface>();
        foreach ( var classType in classTypes )
        {
            var interfaceTypes = Util.Helpers.Reflection.GetInterfaceTypes(classType, typeof(TDependencyInterface));
            if ( interfaceTypes.Count == 0 )
            {
                // 如果类没有实现任何继承自TDependencyInterface的接口，但直接实现了TDependencyInterface本身
                // 则注册类本身（自注册）
                result.Add((classType, classType));
                Console.WriteLine($"[DependencyServiceRegistrar] 注册自注册服务: {classType.Name} 作为 {classType.Name}");
            }
            else
            {
                interfaceTypes.ForEach(interfaceType => 
                {
                    result.Add((interfaceType, classType));
                    Console.WriteLine($"[DependencyServiceRegistrar] 注册服务: {classType.Name} 作为 {interfaceType.Name}");
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 过滤类型
    /// </summary>
    private List<(Type, Type)> FilterTypes(List<(Type, Type)> types)
    {
        var result = new List<(Type, Type)>();
        foreach ( var group in types.GroupBy(t => t.Item1) )
        {
            if ( group.Count() == 1 )
            {
                result.Add(group.First());
                continue;
            }

            result.Add(GetTypesByPriority(group));
        }

        return result;
    }

    /// <summary>
    /// 获取优先级类型
    /// </summary>
    private (Type, Type) GetTypesByPriority(IGrouping<Type, (Type, Type)> group)
    {
        Int32? currentPriority = null;
        Type classType = null;
        foreach ( var item in group )
        {
            var priority = GetPriority(item.Item2);
            if ( currentPriority == null || priority > currentPriority )
            {
                currentPriority = priority;
                classType = item.Item2;
            }
        }

        return (group.Key, classType);
    }

    /// <summary>
    /// 获取优先级
    /// </summary>
    private Int32 GetPriority(Type type)
    {
        var attribute = type.GetCustomAttribute<IocAttribute>();
        if ( attribute == null )
            return 0;
        return attribute.Priority;
    }

    /// <summary>
    /// 注册类型
    /// </summary>
    private void RegisterType(IServiceCollection services, Type interfaceType, Type classType, ServiceLifetime lifetime)
    {
        Console.WriteLine($"[DependencyServiceRegistrar] 尝试注册: {classType.Name} 作为 {interfaceType.Name} ({lifetime})");
        services.TryAdd(new ServiceDescriptor(interfaceType, classType, lifetime));
    }
}
