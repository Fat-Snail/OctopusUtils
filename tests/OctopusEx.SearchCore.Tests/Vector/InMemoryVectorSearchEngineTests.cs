namespace OctopusEx.SearchCore.Tests.Vector;

using Octopus.SearchCore.Vector;

public class InMemoryVectorSearchEngineTests
{
    private static VectorDocument Doc(String id, params Single[] vec)
        => new(id, vec, new Dictionary<String, Object?> { ["id"] = id });

    [Fact]
    public async Task IndexAndSearch_RoundTrips()
    {
        var engine = new InMemoryVectorSearchEngine();
        await engine.IndexAsync(Doc("a", 1, 0));
        await engine.IndexAsync(Doc("b", 0, 1));
        await engine.IndexAsync(Doc("c", 1, 1));

        var hits = await engine.SearchAsync(new Single[] { 1, 0 }, topK: 2);

        hits.Should().HaveCount(2);
        hits[0].Id.Should().Be("a");          // 完全匹配
        hits[0].Score.Should().BeApproximately(1.0f, 1e-6f);
        hits[1].Id.Should().Be("c");          // 45 度
    }

    [Fact]
    public async Task IndexBatch_AndCount()
    {
        var engine = new InMemoryVectorSearchEngine();
        await engine.IndexBatchAsync(new[] { Doc("x", 1, 0), Doc("y", 0, 1) });
        (await engine.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var engine = new InMemoryVectorSearchEngine();
        await engine.IndexAsync(Doc("z", 1, 0));
        (await engine.DeleteAsync("z")).Should().BeTrue();
        (await engine.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsFalse()
    {
        var engine = new InMemoryVectorSearchEngine();
        (await engine.DeleteAsync("missing")).Should().BeFalse();
    }

    [Fact]
    public async Task Search_TopKLimitsResults()
    {
        var engine = new InMemoryVectorSearchEngine();
        for (var i = 0; i < 10; i++)
            await engine.IndexAsync(Doc($"d{i}", i, 0));

        var hits = await engine.SearchAsync(new Single[] { 5, 0 }, topK: 3);
        hits.Should().HaveCount(3);
    }

    [Fact]
    public async Task Index_ReplacesByIdUpsert()
    {
        var engine = new InMemoryVectorSearchEngine();
        await engine.IndexAsync(Doc("a", 1, 0));
        await engine.IndexAsync(Doc("a", 0, 1));

        (await engine.CountAsync()).Should().Be(1);
        var hits = await engine.SearchAsync(new Single[] { 0, 1 }, topK: 1);
        hits[0].Score.Should().BeApproximately(1.0f, 1e-6f);
    }

    [Fact]
    public void VectorDocument_RejectsEmptyVector()
    {
        var act = () => new VectorDocument("id", ReadOnlyMemory<Single>.Empty);
        act.Should().Throw<ArgumentException>();
    }
}
