namespace Octopus.Segment.Abstractions;

/// <summary>
/// 中文分词器抽象。允许在 JiebaSegmenter / PanguSegmenter 等具体实现间运行时切换。
/// </summary>
public interface IChineseSegmenter
{
    /// <summary>切词。返回词条序列。</summary>
    IEnumerable<String> Cut(String text);

    /// <summary>切词（带 POS 词性标注）。返回 (word, pos) 二元组。</summary>
    IEnumerable<TaggedWord> CutWithTags(String text);

    /// <summary>动态添加用户词典词条。可调用多次累计。</summary>
    void AddWord(String word, Int32 freq = 0, String? tag = null);
}

/// <summary>带词性的分词结果</summary>
public sealed record TaggedWord(String Word, String Tag);
