namespace OctopusEx.Segment.Tests.Discovery;

using Octopus.Segment.Abstractions;
using Octopus.Segment.Discovery;

public class NewWordDiscoveryTests
{
    /// <summary>
    /// 把每个 CJK 字符切成单字的 stub 分词器；其他字符按空白分隔。
    /// 用于让新词发现算法的测试与具体词典无关，可重现。
    /// </summary>
    private sealed class CharSplittingStubSegmenter : IChineseSegmenter
    {
        public IEnumerable<String> Cut(String text)
        {
            foreach (var ch in text)
            {
                if (Char.IsWhiteSpace(ch)) continue;
                yield return ch.ToString();
            }
        }
        public IEnumerable<TaggedWord> CutWithTags(String text) => throw new NotImplementedException();
        public void AddWord(String word, Int32 freq = 0, String? tag = null) { }
    }

    [Fact]
    public void Discover_FindsHighFrequencyCandidates()
    {
        var discovery = new NewWordDiscovery(new CharSplittingStubSegmenter());

        var corpus = Enumerable.Repeat("章鱼工具集", 5).ToList();

        var candidates = discovery.Discover(corpus, minFrequency: 3, minLength: 2, maxLength: 5);

        candidates.Should().NotBeEmpty();
        candidates.Should().Contain(c => c.Word == "章鱼" && c.Frequency >= 5);
        candidates.Should().Contain(c => c.Word == "章鱼工具集" && c.Frequency >= 5);
    }

    [Fact]
    public void Discover_RespectsMinFrequencyThreshold()
    {
        var discovery = new NewWordDiscovery(new CharSplittingStubSegmenter());

        var candidates = discovery.Discover(new[] { "章鱼工具集" }, minFrequency: 5);
        candidates.Should().BeEmpty();
    }

    [Fact]
    public void Discover_HandlesEmptyAndNullCorpus()
    {
        var discovery = new NewWordDiscovery(new CharSplittingStubSegmenter());

        var candidates = discovery.Discover(new[] { "", "  ", "正常" }, minFrequency: 1);
        candidates.Should().NotBeNull();
    }

    [Fact]
    public void Discover_OrdersByFrequencyDescending()
    {
        var discovery = new NewWordDiscovery(new CharSplittingStubSegmenter());

        // "章鱼" 出现 6 次（每个 "章鱼工具" 含一次），"工具" 出现 4 次
        var corpus = Enumerable.Repeat("章鱼工具", 4)
            .Concat(Enumerable.Repeat("章鱼", 2))
            .ToList();

        var candidates = discovery.Discover(corpus, minFrequency: 2, minLength: 2, maxLength: 2);

        candidates.Should().BeInDescendingOrder(c => c.Frequency);
    }
}
