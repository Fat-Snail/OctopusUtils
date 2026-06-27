namespace Octopus.SearchCore.Vector;

/// <summary>
/// 向量搜索引擎抽象。具体实现可为内存、Milvus、Qdrant、PostgreSQL pgvector 等。
/// </summary>
public interface IVectorSearchEngine
{
    /// <summary>插入或覆盖一个向量文档（Upsert）。</summary>
    Task IndexAsync(VectorDocument document, CancellationToken cancellationToken = default);

    /// <summary>批量插入或覆盖。实现可优化为批写入。</summary>
    Task IndexBatchAsync(IEnumerable<VectorDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>按 Id 删除。</summary>
    Task<Boolean> DeleteAsync(String id, CancellationToken cancellationToken = default);

    /// <summary>语义相似度搜索，返回 topK 个命中。Score 为相似度（越大越相似）。</summary>
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        ReadOnlyMemory<Single> queryVector,
        Int32 topK,
        CancellationToken cancellationToken = default);

    /// <summary>当前索引文档数。</summary>
    Task<Int32> CountAsync(CancellationToken cancellationToken = default);
}
