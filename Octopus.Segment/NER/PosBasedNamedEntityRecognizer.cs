namespace Octopus.Segment.NER;

using System.Text.RegularExpressions;
using Abstractions;

/// <summary>
/// 基于词性标注 + 规则的 NER。
///
/// 实现思路：
/// - 用 IChineseSegmenter.CutWithTags 拿到词性标注序列
/// - 按 POS 标签映射实体类型：nr→人名, ns→地名, nt→机构名, t→时间
/// - 对时间表达式额外用正则补充识别（年月日 / 今天 / 昨天 / 5 月 1 日 等）
/// - 合并相邻同类实体（如"中国 人民 银行"被切成多段时合并为机构名）
/// </summary>
public class PosBasedNamedEntityRecognizer : INamedEntityRecognizer
{
    private readonly IChineseSegmenter _segmenter;

    private static readonly Regex TimePatterns = new(
        @"(?:\d{2,4}\s*年\s*\d{1,2}\s*月\s*\d{1,2}\s*日)" +
        @"|(?:\d{1,2}\s*月\s*\d{1,2}\s*日)" +
        @"|(?:\d{4}\s*[-/]\s*\d{1,2}\s*[-/]\s*\d{1,2})" +
        @"|(?:今天|昨天|明天|后天|前天|今早|今晚|今夜)" +
        @"|(?:\d{1,2}\s*[:：]\s*\d{1,2}(?:\s*[:：]\s*\d{1,2})?)",
        RegexOptions.Compiled);

    public PosBasedNamedEntityRecognizer(IChineseSegmenter segmenter)
        => _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));

    public IReadOnlyList<NamedEntity> Recognize(String text)
    {
        if (String.IsNullOrEmpty(text)) return Array.Empty<NamedEntity>();

        var entities = new List<NamedEntity>();
        var cursor = 0;

        // 第一步：POS 驱动的实体识别
        var taggedWords = _segmenter.CutWithTags(text).ToList();
        EntityType? currentType = null;
        var currentStart = 0;
        var currentBuilder = new System.Text.StringBuilder();

        foreach (var tw in taggedWords)
        {
            // 在原文中定位词的起点（按顺序前进）
            var idx = text.IndexOf(tw.Word, cursor, StringComparison.Ordinal);
            if (idx < 0) { cursor += tw.Word.Length; continue; }
            cursor = idx + tw.Word.Length;

            var mapped = MapPosTag(tw.Tag);
            if (mapped == currentType && mapped != null)
            {
                // 累积同类相邻词
                currentBuilder.Append(tw.Word);
            }
            else
            {
                FlushCurrent(entities, currentType, currentStart, currentBuilder);
                currentType = mapped;
                currentStart = idx;
                currentBuilder.Clear();
                if (mapped != null) currentBuilder.Append(tw.Word);
            }
        }
        FlushCurrent(entities, currentType, currentStart, currentBuilder);

        // 第二步：正则补充时间识别（POS 容易漏掉日期数字）
        foreach (Match match in TimePatterns.Matches(text))
        {
            var start = match.Index;
            var end = start + match.Length;
            if (entities.Any(e => Overlaps(e.StartIndex, e.EndIndex, start, end))) continue;
            entities.Add(new NamedEntity(EntityType.Time, match.Value, start, end));
        }

        return entities.OrderBy(e => e.StartIndex).ToList();
    }

    private static void FlushCurrent(
        List<NamedEntity> entities, EntityType? type, Int32 start, System.Text.StringBuilder builder)
    {
        if (type == null || builder.Length == 0) return;
        var text = builder.ToString();
        entities.Add(new NamedEntity(type.Value, text, start, start + text.Length));
    }

    private static EntityType? MapPosTag(String tag) => tag switch
    {
        "nr" or "nrfg" or "nrt" => EntityType.Person,
        "ns" => EntityType.Place,
        "nt" => EntityType.Organization,
        "t" => EntityType.Time,
        _ => null,
    };

    private static Boolean Overlaps(Int32 aStart, Int32 aEnd, Int32 bStart, Int32 bEnd)
        => aStart < bEnd && bStart < aEnd;
}
