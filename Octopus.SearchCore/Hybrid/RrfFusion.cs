namespace Octopus.SearchCore.Hybrid;

/// <summary>
/// 倒数排名融合（Reciprocal Rank Fusion）。
/// 论文：Cormack et al. 2009. 不依赖原始分数的尺度，仅按各路结果的排名计算融合分。
/// 公式：score(d) = Σ_i  1 / (k + rank_i(d))，rank 从 1 开始；缺失则跳过该路。
/// 默认 k = 60（论文建议值）。
/// </summary>
public static class RrfFusion
{
    /// <summary>
    /// 融合多路命中列表。
    /// </summary>
    /// <param name="rankedLists">多路检索结果，每路为按相关性降序的 Id 列表</param>
    /// <param name="topK">返回的 top-K 条目数</param>
    /// <param name="k">RRF 平滑参数，默认 60</param>
    public static IReadOnlyList<FusionHit> Fuse(
        IEnumerable<IReadOnlyList<String>> rankedLists,
        Int32 topK,
        Int32 k = 60)
    {
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK));

        var scores = new Dictionary<String, Double>();

        foreach (var list in rankedLists)
        {
            for (var rank = 0; rank < list.Count; rank++)
            {
                var id = list[rank];
                var contribution = 1.0 / (k + rank + 1);
                scores[id] = scores.TryGetValue(id, out var existing) ? existing + contribution : contribution;
            }
        }

        return scores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topK)
            .Select(kvp => new FusionHit(kvp.Key, kvp.Value))
            .ToList();
    }
}

public sealed record FusionHit(String Id, Double Score);
