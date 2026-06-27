namespace OctopusEx.SearchCore.Tests.Vector;

using Octopus.SearchCore.Vector;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_Returns1()
    {
        Single[] a = { 1, 2, 3 };
        VectorMath.CosineSimilarity(a, a).Should().BeApproximately(1.0f, 1e-6f);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_ReturnsNegative1()
    {
        Single[] a = { 1, 0 };
        Single[] b = { -1, 0 };
        VectorMath.CosineSimilarity(a, b).Should().BeApproximately(-1.0f, 1e-6f);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_Returns0()
    {
        Single[] a = { 1, 0 };
        Single[] b = { 0, 1 };
        VectorMath.CosineSimilarity(a, b).Should().BeApproximately(0f, 1e-6f);
    }

    [Fact]
    public void CosineSimilarity_ZeroVector_Returns0()
    {
        Single[] a = { 0, 0, 0 };
        Single[] b = { 1, 2, 3 };
        VectorMath.CosineSimilarity(a, b).Should().Be(0);
    }

    [Fact]
    public void CosineSimilarity_LengthMismatch_Throws()
    {
        Single[] a = { 1 };
        Single[] b = { 1, 2 };
        var act = () => VectorMath.CosineSimilarity(a, b);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizeInPlace_ProducesUnitVector()
    {
        Single[] v = { 3, 4 };
        VectorMath.NormalizeInPlace(v);
        v[0].Should().BeApproximately(0.6f, 1e-6f);
        v[1].Should().BeApproximately(0.8f, 1e-6f);
    }

    [Fact]
    public void NormalizeInPlace_ZeroVector_Untouched()
    {
        Single[] v = { 0, 0, 0 };
        VectorMath.NormalizeInPlace(v);
        v.Should().AllSatisfy(x => x.Should().Be(0));
    }
}
