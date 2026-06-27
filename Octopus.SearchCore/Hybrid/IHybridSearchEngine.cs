namespace Octopus.SearchCore.Hybrid;

/// <summary>
/// 混合检索引擎：关键词（BM25 / Lucene）+ 语义（向量余弦）双路召回，RRF 重排序。
/// </summary>
public interface IHybridSearchEngine
{
    /// <summary>
    /// 用查询文本同时驱动两路检索并融合。
    /// </summary>
    /// <param name="queryText">查询文本</param>
    /// <param name="topK">返回前 K 条</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<FusionHit>> SearchAsync(
        String queryText,
        Int32 topK,
        CancellationToken cancellationToken = default);
}
