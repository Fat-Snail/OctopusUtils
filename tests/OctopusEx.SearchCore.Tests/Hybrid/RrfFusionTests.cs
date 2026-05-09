namespace OctopusEx.SearchCore.Tests.Hybrid;

using Octopus.SearchCore.Hybrid;

public class RrfFusionTests
{
    [Fact]
    public void Fuse_SingleList_RanksAsInput()
    {
        var list = new List<String> { "a", "b", "c" };
        var fused = RrfFusion.Fuse(new[] { (IReadOnlyList<String>)list }, topK: 3);

        fused.Should().HaveCount(3);
        fused[0].Id.Should().Be("a");
        fused[2].Id.Should().Be("c");
    }

    [Fact]
    public void Fuse_TwoLists_BoostsItemsAppearingInBoth()
    {
        var keyword = new List<String> { "x", "y", "z" };
        var vector  = new List<String> { "x", "z", "w" };

        var fused = RrfFusion.Fuse(new[] { (IReadOnlyList<String>)keyword, vector }, topK: 4);

        // x 在两路 rank 0，得分最高
        fused[0].Id.Should().Be("x");
        // z 出现在两路（rank 2 + rank 1），应排在仅出现一次的 y / w 之前
        fused[1].Id.Should().Be("z");
    }

    [Fact]
    public void Fuse_RespectsTopK()
    {
        var list = Enumerable.Range(0, 100).Select(i => i.ToString()).ToList();
        var fused = RrfFusion.Fuse(new[] { (IReadOnlyList<String>)list }, topK: 5);
        fused.Should().HaveCount(5);
    }

    [Fact]
    public void Fuse_HigherKMakesScoresMoreEqual()
    {
        // 构造一项排名稳居首位、另两项各仅出现一次的场景，便于观察分数差
        var a = new List<String> { "1", "2" };
        var b = new List<String> { "1", "3" };

        var smallK = RrfFusion.Fuse(new[] { (IReadOnlyList<String>)a, b }, topK: 3, k: 1);
        var largeK = RrfFusion.Fuse(new[] { (IReadOnlyList<String>)a, b }, topK: 3, k: 1000);

        var smallDelta = smallK[0].Score - smallK[1].Score;
        var largeDelta = largeK[0].Score - largeK[1].Score;

        largeDelta.Should().BeLessThan(smallDelta);
    }
}
