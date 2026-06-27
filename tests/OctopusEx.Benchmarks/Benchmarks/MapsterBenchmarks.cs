namespace OctopusEx.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using Mapster;
using OctopusEx.WebCore.DomainCore.Mapping;

[MemoryDiagnoser]
public class MapsterBenchmarks
{
    private readonly IObjectMapper _mapper = new MapsterObjectMapper(new TypeAdapterConfig());

    private readonly Source _src = new(1, "Alice", 30);
    private readonly List<Source> _list = Enumerable.Range(0, 100)
        .Select(i => new Source(i, $"u{i}", i)).ToList();

    public record Source(Int32 Id, String Name, Int32 Age);
    public class Dest { public Int32 Id { get; set; } public String? Name { get; set; } public Int32 Age { get; set; } }

    [Benchmark]
    public Dest Map_Single() => _mapper.Map<Source, Dest>(_src);

    [Benchmark]
    public List<Dest> Map_List_100() => _mapper.MapList<Source, Dest>(_list);
}
