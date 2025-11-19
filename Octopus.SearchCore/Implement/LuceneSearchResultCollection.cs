using Octopus.SearchCore.Interfaces;

namespace Octopus.SearchCore;

/// <summary>
/// 搜索结果集
/// </summary>
public class LuceneSearchResultCollection : ILuceneSearchResultCollection
{
    /// <summary>
    /// 结果集
    /// </summary>
    public IList<ILuceneSearchResult> Results { get; set; } = new List<ILuceneSearchResult>();

    /// <summary>
    /// 耗时
    /// </summary>
    public Int64 Elapsed { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public Int32 TotalHits { get; set; }
}