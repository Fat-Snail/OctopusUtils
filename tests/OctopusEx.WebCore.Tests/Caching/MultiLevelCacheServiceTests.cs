namespace OctopusEx.WebCore.Tests.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OctopusEx.WebCore.Caching;

public class MultiLevelCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly InMemoryDistributedCache _l2Backing = new();
    private readonly MemoryCacheService _l1;
    private readonly DistributedCacheService _l2;
    private readonly MultiLevelCacheService _multi;
    private readonly CacheOptions _options = new() { TtlJitterPercent = 0 };

    public MultiLevelCacheServiceTests()
    {
        _l1 = new MemoryCacheService(_memoryCache, _options);
        _l2 = new DistributedCacheService(_l2Backing, _options, NullLogger<DistributedCacheService>.Instance);
        _multi = new MultiLevelCacheService(_l1, _l2, NullLogger<MultiLevelCacheService>.Instance);
    }

    public void Dispose()
    {
        _l1.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task SetAsync_WritesBothLevels()
    {
        await _multi.SetAsync("k", "v");

        (await _l1.GetAsync<String>("k")).Should().Be("v");
        (await _l2.GetAsync<String>("k")).Should().Be("v");
    }

    [Fact]
    public async Task GetAsync_L1Miss_L2Hit_BackfillsL1()
    {
        await _l2.SetAsync("k", "v");
        (await _l1.GetAsync<String>("k")).Should().BeNull();

        var v = await _multi.GetAsync<String>("k");

        v.Should().Be("v");
        (await _l1.GetAsync<String>("k")).Should().Be("v");
    }

    [Fact]
    public async Task RemoveAsync_RemovesBothLevels()
    {
        await _multi.SetAsync("k", "v");
        await _multi.RemoveAsync("k");

        (await _l1.GetAsync<String>("k")).Should().BeNull();
        (await _l2.GetAsync<String>("k")).Should().BeNull();
    }

    [Fact]
    public async Task GetOrAddAsync_FactoryRunsOnce_AcrossLevels()
    {
        var calls = 0;
        Task<String?> Factory(CancellationToken _) { Interlocked.Increment(ref calls); return Task.FromResult<String?>("v"); }

        await _multi.GetOrAddAsync("k", Factory);
        await _multi.GetOrAddAsync("k", Factory);
        // 清掉 L1，再次请求应从 L2 命中
        await _l1.RemoveAsync("k");
        await _multi.GetOrAddAsync("k", Factory);

        calls.Should().Be(1);
    }

    /// <summary>
    /// 简单的 IDistributedCache 内存实现，避免引入 Redis 依赖。
    /// </summary>
    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<String, Byte[]> _store = new();
        private readonly Object _lock = new();

        public Byte[]? Get(String key) { lock (_lock) return _store.TryGetValue(key, out var v) ? v : null; }
        public Task<Byte[]?> GetAsync(String key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Set(String key, Byte[] value, DistributedCacheEntryOptions options) { lock (_lock) _store[key] = value; }
        public Task SetAsync(String key, Byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
        public void Refresh(String key) { }
        public Task RefreshAsync(String key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(String key) { lock (_lock) _store.Remove(key); }
        public Task RemoveAsync(String key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
    }
}
