using Octopus.SearchCore.Interfaces;

namespace Octopus.SearchCore;

/// <summary>
/// 搜索结果集
/// </summary>
/// <typeparam name="T"></typeparam>
public class ScoredSearchResultCollection<T> : IScoredSearchResultCollection<T>
{
    /// <summary>
    /// 结果集
    /// </summary>
    public IList<IScoredSearchResult<T>> Results { get; set; } = new List<IScoredSearchResult<T>>();

    /// <summary>
    /// 耗时
    /// </summary>
    public Int64 Elapsed { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public Int32 TotalHits { get; set; }
}