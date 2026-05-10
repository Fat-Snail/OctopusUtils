namespace OctopusEx.Benchmarks.Benchmarks;

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Caching.Memory;
using OctopusEx.WebCore.Caching;

[MemoryDiagnoser]
public class CacheBenchmarks
{
    private MemoryCache _memoryCache = null!;
    private MemoryCacheService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _service = new MemoryCacheService(_memoryCache, new CacheOptions { TtlJitterPercent = 0 });
        _service.SetAsync("warm-key", "warm-value").GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() { _service.Dispose(); _memoryCache.Dispose(); }

    [Benchmark]
    public async Task<String?> GetAsync_Hit() => await _service.GetAsync<String>("warm-key");

    [Benchmark]
    public async Task<String?> GetAsync_Miss() => await _service.GetAsync<String>("never-set");

    [Benchmark]
    public async Task<String?> GetOrAddAsync_Hit()
        => await _service.GetOrAddAsync<String>("warm-key", _ => Task.FromResult<String?>("x"));
}
