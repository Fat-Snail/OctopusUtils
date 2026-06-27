namespace OctopusEx.WebCore.DomainCore.Mapping;

using Mapster;

/// <summary>
/// 基于 Mapster 的对象映射实现。
/// 通过注入的 TypeAdapterConfig 支持自定义映射规则与多 Profile。
/// </summary>
public class MapsterObjectMapper : IObjectMapper
{
    private readonly TypeAdapterConfig _config;

    public MapsterObjectMapper(TypeAdapterConfig config) => _config = config;

    public TDestination Map<TDestination>(Object source) => source.Adapt<TDestination>(_config);

    public TDestination Map<TSource, TDestination>(TSource source) => source.Adapt<TSource, TDestination>(_config);

    public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
        source.Adapt(destination, _config);

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source) =>
        source.Adapt<List<TDestination>>(_config);

    public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source) =>
        source.ProjectToType<TDestination>(_config);
}
