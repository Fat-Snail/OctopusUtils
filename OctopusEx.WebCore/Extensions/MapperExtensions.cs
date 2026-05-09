namespace OctopusEx.WebCore.Extensions;

using DomainCore.Mapping;

/// <summary>
/// 对象映射注册扩展
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    /// 注册 Mapster 对象映射器。注册后 CrudServiceBase 子类无需重写
    /// MapToDto / MapToEntity / UpdateEntityFromDto 即可完成基础 CRUD 映射。
    /// </summary>
    public static IServiceCollection AddSimpleMapper(this IServiceCollection services)
    {
        services.AddSingleton<IObjectMapper, MapsterObjectMapper>();
        return services;
    }
}
