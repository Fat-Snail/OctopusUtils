namespace Octopus.Segment.Abstractions;

using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;

/// <summary>
/// 把现有 JiebaNet.Segmenter 包装为 IChineseSegmenter。
/// 并发说明：JiebaSegmenter 自身是线程安全的（基于不可变字典 + 锁）；本适配器无需额外同步。
/// </summary>
public class JiebaSegmenterAdapter : IChineseSegmenter
{
    private readonly JiebaSegmenter _segmenter;
    private readonly PosSegmenter _posSegmenter;

    public JiebaSegmenterAdapter() : this(new JiebaSegmenter()) { }

    public JiebaSegmenterAdapter(JiebaSegmenter segmenter)
    {
        _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));
        _posSegmenter = new PosSegmenter(_segmenter);
    }

    /// <summary>访问内部 JiebaSegmenter，用于高级场景</summary>
    public JiebaSegmenter Inner => _segmenter;

    public IEnumerable<String> Cut(String text) => _segmenter.Cut(text);

    public IEnumerable<TaggedWord> CutWithTags(String text)
        => _posSegmenter.Cut(text).Select(p => new TaggedWord(p.Word, p.Flag));

    public void AddWord(String word, Int32 freq = 0, String? tag = null)
        => _segmenter.AddWord(word, freq, tag);
}
