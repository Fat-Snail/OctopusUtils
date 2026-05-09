namespace Octopus.SearchCore.Vector;

/// <summary>
/// 向量数学工具。仅依赖 ReadOnlySpan，避免在热路径分配数组。
/// </summary>
public static class VectorMath
{
    /// <summary>
    /// 余弦相似度 ∈ [-1, 1]，1 表示方向完全一致。
    /// 对零向量返回 0。
    /// </summary>
    public static Single CosineSimilarity(ReadOnlySpan<Single> a, ReadOnlySpan<Single> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException($"Vector length mismatch: {a.Length} vs {b.Length}");

        Single dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0) return 0;
        return dot / (Single)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>对向量做 L2 归一化，原地写入；零向量保持原样。</summary>
    public static void NormalizeInPlace(Span<Single> vector)
    {
        Single sumSquares = 0;
        for (var i = 0; i < vector.Length; i++) sumSquares += vector[i] * vector[i];
        if (sumSquares == 0) return;

        var norm = (Single)Math.Sqrt(sumSquares);
        for (var i = 0; i < vector.Length; i++) vector[i] /= norm;
    }
}
