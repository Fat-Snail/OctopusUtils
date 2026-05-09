namespace OctopusEx.WebCore.DomainCore.Mapping;

using Mapster;

/// <summary>
/// 基于 Mapster 的对象映射实现
/// </summary>
public class MapsterObjectMapper : IObjectMapper
{
    public TDestination Map<TDestination>(Object source) => source.Adapt<TDestination>();

    public TDestination Map<TSource, TDestination>(TSource source) => source.Adapt<TDestination>();

    public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
        source.Adapt(destination);
}
