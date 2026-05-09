namespace OctopusEx.WebCore.DomainCore.Mapping;

/// <summary>
/// 对象映射抽象接口
/// </summary>
public interface IObjectMapper
{
    /// <summary>
    /// 将源对象映射为目标类型
    /// </summary>
    TDestination Map<TDestination>(Object source);

    /// <summary>
    /// 将源对象映射为目标类型（强类型版本）
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// 将源对象的属性映射到已有目标对象上
    /// </summary>
    void Map<TSource, TDestination>(TSource source, TDestination destination);
}
