using System.Collections.Generic;

namespace Octopus.SearchCore.Interfaces
{
    /// <summary>
    /// 搜索结果集
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IScoredSearchResultCollection<T>
    {
        /// <summary>
        /// 总条数
        /// </summary>
        Int32 TotalHits { get; set; }

        /// <summary>
        /// 耗时
        /// </summary>
        Int64 Elapsed { get; set; }

        /// <summary>
        /// 结果集
        /// </summary>
        IList<IScoredSearchResult<T>> Results { get; set; }
    }
}