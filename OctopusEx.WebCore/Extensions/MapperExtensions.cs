namespace OctopusEx.WebCore.Extensions;

using DomainCore.Mapping;
using Mapster;

/// <summary>
/// 对象映射注册扩展
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    /// 注册 Mapster 对象映射器。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">映射配置回调，用于注册 TypeAdapter 规则、扫描 IRegister 等</param>
    public static IServiceCollection AddSimpleMapper(
        this IServiceCollection services,
        Action<TypeAdapterConfig>? configure = null)
    {
        var config = new TypeAdapterConfig();
        configure?.Invoke(config);

        services.AddSingleton(config);
        services.AddSingleton<IObjectMapper, MapsterObjectMapper>();
        return services;
    }
}
