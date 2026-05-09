namespace Octopus.SearchCore.Vector;

using System.Collections.Concurrent;

/// <summary>
/// 内存版向量搜索引擎。基于余弦相似度的暴力扫描，适合：
/// - 单元测试 / 本地开发
/// - 小规模数据（&lt; 10 万条）
/// 生产环境请用 Milvus / Qdrant / pgvector 实现。
/// </summary>
public class InMemoryVectorSearchEngine : IVectorSearchEngine
{
    private readonly ConcurrentDictionary<String, VectorDocument> _store = new();

    public Task IndexAsync(VectorDocument document, CancellationToken cancellationToken = default)
    {
        _store[document.Id] = document;
        return Task.CompletedTask;
    }

    public Task IndexBatchAsync(IEnumerable<VectorDocument> documents, CancellationToken cancellationToken = default)
    {
        foreach (var doc in documents) _store[doc.Id] = doc;
        return Task.CompletedTask;
    }

    public Task<Boolean> DeleteAsync(String id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryRemove(id, out _));

    public Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        ReadOnlyMemory<Single> queryVector,
        Int32 topK,
        CancellationToken cancellationToken = default)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));

        var queryArr = queryVector.Span;
        var hits = new List<VectorSearchHit>(_store.Count);

        foreach (var kvp in _store)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var doc = kvp.Value;
            var score = VectorMath.CosineSimilarity(queryArr, doc.Vector.Span);
            hits.Add(new VectorSearchHit(doc.Id, score, doc.Metadata));
        }

        IReadOnlyList<VectorSearchHit> result = hits
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Int32> CountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Count);
}
