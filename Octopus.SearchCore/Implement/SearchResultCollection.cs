using Octopus.SearchCore.Interfaces;

namespace Octopus.SearchCore;

/// <summary>
/// 搜索结果集
/// </summary>
/// <typeparam name="T"></typeparam>
public class SearchResultCollection<T> : ISearchResultCollection<T>
{
    /// <summary>
    /// 实体集
    /// </summary>
    public IList<T> Results { get; set; }

    /// <summary>
    /// 耗时
    /// </summary>
    public Int64 Elapsed { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public Int32 TotalHits { get; set; }

    public SearchResultCollection()
    {
        Results = new List<T>();
    }
}