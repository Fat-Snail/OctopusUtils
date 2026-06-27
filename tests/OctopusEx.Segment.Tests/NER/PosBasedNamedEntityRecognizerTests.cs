namespace OctopusEx.Segment.Tests.NER;

using Octopus.Segment.Abstractions;
using Octopus.Segment.NER;

public class PosBasedNamedEntityRecognizerTests
{
    private readonly INamedEntityRecognizer _ner;

    public PosBasedNamedEntityRecognizerTests()
    {
        _ner = new PosBasedNamedEntityRecognizer(new JiebaSegmenterAdapter());
    }

    [Fact]
    public void Recognize_DateExpression_ProducesTimeEntity()
    {
        var entities = _ner.Recognize("我们将在2025年11月12日举行发布会");

        entities.Should().Contain(e => e.Type == EntityType.Time && e.Text.Contains("2025"));
    }

    [Fact]
    public void Recognize_RelativeTime_ProducesTimeEntity()
    {
        var entities = _ner.Recognize("今天天气很好，明天会下雨");

        entities.Should().Contain(e => e.Type == EntityType.Time && e.Text == "今天");
        entities.Should().Contain(e => e.Type == EntityType.Time && e.Text == "明天");
    }

    [Fact]
    public void Recognize_TimeOfDay_ProducesTimeEntity()
    {
        var entities = _ner.Recognize("会议安排在 14:30 开始");

        entities.Should().Contain(e => e.Type == EntityType.Time && e.Text.Replace(" ", "").Contains("14:30"));
    }

    [Fact]
    public void Recognize_EmptyText_ReturnsEmpty()
    {
        _ner.Recognize("").Should().BeEmpty();
        _ner.Recognize(null!).Should().BeEmpty();
    }

    [Fact]
    public void Recognize_ResultsAreOrderedByPosition()
    {
        var entities = _ner.Recognize("今天去北京，明天去上海");

        for (var i = 1; i < entities.Count; i++)
            entities[i].StartIndex.Should().BeGreaterThanOrEqualTo(entities[i - 1].StartIndex);
    }
}
