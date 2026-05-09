namespace OctopusEx.Segment.Tests.Abstractions;

using Octopus.Segment.Abstractions;

public class JiebaSegmenterAdapterTests
{
    [Fact]
    public void Cut_ProducesNonEmptyTokens()
    {
        var seg = new JiebaSegmenterAdapter();
        var tokens = seg.Cut("我爱北京天安门").ToList();
        tokens.Should().NotBeEmpty();
        String.Concat(tokens).Should().Be("我爱北京天安门");
    }

    [Fact]
    public void CutWithTags_ReturnsPosTags()
    {
        var seg = new JiebaSegmenterAdapter();
        var tagged = seg.CutWithTags("我爱北京").ToList();

        tagged.Should().NotBeEmpty();
        tagged.Should().AllSatisfy(t => t.Tag.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public void AddWord_NewWord_AppearsInCutOutput()
    {
        var seg = new JiebaSegmenterAdapter();
        seg.AddWord("章鱼工具集", 1000);

        var tokens = seg.Cut("章鱼工具集是个好库").ToList();
        tokens.Should().Contain("章鱼工具集");
    }
}
