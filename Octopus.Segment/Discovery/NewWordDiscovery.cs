namespace Octopus.Segment.Discovery;

using Abstractions;

/// <summary>
/// 基于词频统计的新词发现。
/// 算法：用现有 segmenter 切词，找出"未登录单字组成的连续片段"作为新词候选，统计频次后按阈值过滤。
/// 适合场景：批量分析语料后建议加入用户词典；不做精细 PMI / 凝固度计算。
/// </summary>
public class NewWordDiscovery
{
    private readonly IChineseSegmenter _segmenter;

    public NewWordDiscovery(IChineseSegmenter segmenter)
        => _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));

    /// <summary>
    /// 扫描语料，返回候选新词及其频次（降序）。
    /// </summary>
    /// <param name="corpus">语料文档列表</param>
    /// <param name="minFrequency">最小频次阈值（候选词在语料中至少出现 N 次）</param>
    /// <param name="minLength">最小词长（中文字符数）</param>
    /// <param name="maxLength">最大词长，避免长串噪声</param>
    public IReadOnlyList<NewWordCandidate> Discover(
        IEnumerable<String> corpus,
        Int32 minFrequency = 3,
        Int32 minLength = 2,
        Int32 maxLength = 6)
    {
        if (minFrequency < 1) throw new ArgumentOutOfRangeException(nameof(minFrequency));
        if (minLength < 2) throw new ArgumentOutOfRangeException(nameof(minLength));

        var counts = new Dictionary<String, Int32>(StringComparer.Ordinal);

        foreach (var text in corpus)
        {
            if (String.IsNullOrEmpty(text)) continue;

            var tokens = _segmenter.Cut(text).ToList();

            // 收集"连续单字"片段：分词器把未登录词切成单字，连续的单字片段往往是新词
            var buffer = new System.Text.StringBuilder();
            foreach (var token in tokens)
            {
                if (token.Length == 1 && IsCjk(token[0]))
                {
                    buffer.Append(token);
                }
                else
                {
                    EmitSubstrings(buffer.ToString(), counts, minLength, maxLength);
                    buffer.Clear();
                }
            }
            EmitSubstrings(buffer.ToString(), counts, minLength, maxLength);
        }

        return counts
            .Where(kvp => kvp.Value >= minFrequency)
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => new NewWordCandidate(kvp.Key, kvp.Value))
            .ToList();
    }

    private static void EmitSubstrings(String fragment, Dictionary<String, Int32> counts, Int32 minLength, Int32 maxLength)
    {
        if (fragment.Length < minLength) return;
        for (var len = minLength; len <= maxLength; len++)
        {
            for (var i = 0; i + len <= fragment.Length; i++)
            {
                var sub = fragment.Substring(i, len);
                counts[sub] = counts.TryGetValue(sub, out var c) ? c + 1 : 1;
            }
        }
    }

    private static Boolean IsCjk(Char c) => c >= 0x4E00 && c <= 0x9FFF;
}

/// <summary>新词候选</summary>
public sealed record NewWordCandidate(String Word, Int32 Frequency);
