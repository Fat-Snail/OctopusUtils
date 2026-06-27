namespace Octopus.SearchCore.Hybrid;

using Microsoft.Extensions.AI;
using Vector;

/// <summary>
/// 默认混合检索实现。
/// - 关键词路：调用方提供的 keywordSearch（通常基于 Lucene），按文本查询返回 Id 排名列表
/// - 语义路：用 IEmbeddingGenerator 把 query 转向量后调用 IVectorSearchEngine
/// - 融合：RRF
/// </summary>
public class HybridSearchEngine : IHybridSearchEngine
{
    private readonly Func<String, Int32, CancellationToken, Task<IReadOnlyList<String>>> _keywordSearch;
    private readonly IVectorSearchEngine _vectorSearch;
    private readonly IEmbeddingGenerator<String, Embedding<Single>> _embeddings;
    private readonly Int32 _rrfK;

    public HybridSearchEngine(
        Func<String, Int32, CancellationToken, Task<IReadOnlyList<String>>> keywordSearch,
        IVectorSearchEngine vectorSearch,
        IEmbeddingGenerator<String, Embedding<Single>> embeddings,
        Int32 rrfK = 60)
    {
        _keywordSearch = keywordSearch ?? throw new ArgumentNullException(nameof(keywordSearch));
        _vectorSearch = vectorSearch ?? throw new ArgumentNullException(nameof(vectorSearch));
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _rrfK = rrfK;
    }

    public async Task<IReadOnlyList<FusionHit>> SearchAsync(
        String queryText,
        Int32 topK,
        CancellationToken cancellationToken = default)
    {
        // 双路并行召回
        var keywordTask = _keywordSearch(queryText, topK * 2, cancellationToken);
        var vectorTask = SearchVectorAsync(queryText, topK * 2, cancellationToken);

        await Task.WhenAll(keywordTask, vectorTask);

        var keywordIds = keywordTask.Result;
        var vectorIds = vectorTask.Result.Select(h => h.Id).ToList();

        return RrfFusion.Fuse(new[] { keywordIds, (IReadOnlyList<String>)vectorIds }, topK, _rrfK);
    }

    private async Task<IReadOnlyList<VectorSearchHit>> SearchVectorAsync(
        String queryText, Int32 topK, CancellationToken ct)
    {
        var embedding = await _embeddings.GenerateAsync(new[] { queryText }, cancellationToken: ct);
        var vector = embedding[0].Vector;
        return await _vectorSearch.SearchAsync(vector, topK, ct);
    }
}
