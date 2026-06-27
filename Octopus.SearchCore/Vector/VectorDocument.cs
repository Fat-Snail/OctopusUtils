namespace Octopus.SearchCore.Vector;

/// <summary>
/// 向量索引文档。Id 唯一标识；Vector 为向量；Metadata 任意键值对（用于召回阶段过滤或回填业务字段）。
/// </summary>
public sealed class VectorDocument
{
    public String Id { get; }
    public ReadOnlyMemory<Single> Vector { get; }
    public IReadOnlyDictionary<String, Object?> Metadata { get; }

    public VectorDocument(String id, ReadOnlyMemory<Single> vector, IReadOnlyDictionary<String, Object?>? metadata = null)
    {
        if (String.IsNullOrEmpty(id)) throw new ArgumentException("Id must not be empty", nameof(id));
        if (vector.IsEmpty) throw new ArgumentException("Vector must not be empty", nameof(vector));
        Id = id;
        Vector = vector;
        Metadata = metadata ?? new Dictionary<String, Object?>();
    }
}

/// <summary>向量检索命中</summary>
public sealed record VectorSearchHit(String Id, Single Score, IReadOnlyDictionary<String, Object?> Metadata);
