namespace OctopusEx.WebCore.Tests.Caching;

using Microsoft.Extensions.Caching.Memory;
using OctopusEx.WebCore.Caching;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryCacheService _service;
    private readonly CacheOptions _options = new() { TtlJitterPercent = 0, KeyPrefix = "" };

    public MemoryCacheServiceTests()
    {
        _service = new MemoryCacheService(_memoryCache, _options);
    }

    public void Dispose()
    {
        _service.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task GetAsync_AfterSet_ReturnsValue()
    {
        await _service.SetAsync("key", "value");
        var result = await _service.GetAsync<String>("key");
        result.Should().Be("value");
    }

    [Fact]
    public async Task GetAsync_NonExistent_ReturnsDefault()
    {
        var result = await _service.GetAsync<String>("missing");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_RemovesValue()
    {
        await _service.SetAsync("k", 42);
        await _service.RemoveAsync("k");
        (await _service.ExistsAsync("k")).Should().BeFalse();
    }

    [Fact]
    public async Task GetOrAddAsync_CachesValue_FactoryRunsOnce()
    {
        var calls = 0;
        Task<String?> Factory(CancellationToken _) { Interlocked.Increment(ref calls); return Task.FromResult<String?>("computed"); }

        var v1 = await _service.GetOrAddAsync("k", Factory);
        var v2 = await _service.GetOrAddAsync("k", Factory);

        v1.Should().Be("computed");
        v2.Should().Be("computed");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_ConcurrentCalls_FactoryRunsOnce()
    {
        var calls = 0;
        async Task<String?> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(50);
            return "v";
        }

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => _service.GetOrAddAsync("hot", Factory))
            .ToArray();

        await Task.WhenAll(tasks);
        calls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_NullResult_IsCached_WhenCacheNullValuesEnabled()
    {
        _options.CacheNullValues = true;
        var calls = 0;
        Task<String?> Factory(CancellationToken _) { Interlocked.Increment(ref calls); return Task.FromResult<String?>(null); }

        await _service.GetOrAddAsync("nullkey", Factory);
        await _service.GetOrAddAsync("nullkey", Factory);

        calls.Should().Be(1);
    }

    [Fact]
    public async Task KeyPrefix_IsApplied()
    {
        var prefixed = new MemoryCacheService(_memoryCache, new CacheOptions { KeyPrefix = "myapp:" });
        await prefixed.SetAsync("k", "v");
        _memoryCache.TryGetValue("myapp:k", out _).Should().BeTrue();
    }
}
