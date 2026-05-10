namespace OctopusEx.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using Octopus.SearchCore.Vector;

[MemoryDiagnoser]
public class VectorMathBenchmarks
{
    [Params(128, 768, 1536)]
    public Int32 Dim;

    private Single[] _a = null!;
    private Single[] _b = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        _a = Enumerable.Range(0, Dim).Select(_ => (Single)rng.NextDouble()).ToArray();
        _b = Enumerable.Range(0, Dim).Select(_ => (Single)rng.NextDouble()).ToArray();
    }

    [Benchmark]
    public Single Cosine() => VectorMath.CosineSimilarity(_a, _b);
}
