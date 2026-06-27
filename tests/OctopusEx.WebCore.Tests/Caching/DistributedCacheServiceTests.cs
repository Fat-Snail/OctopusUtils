namespace OctopusEx.WebCore.Tests.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using OctopusEx.WebCore.Caching;

public class DistributedCacheServiceTests
{
    private static (DistributedCacheService svc, InMemoryDistributedCache backing) Build()
    {
        var backing = new InMemoryDistributedCache();
        var svc = new DistributedCacheService(backing, new CacheOptions(), NullLogger<DistributedCacheService>.Instance);
        return (svc, backing);
    }

    [Fact]
    public async Task TryGetAsync_DistinguishesMissFromCachedNull()
    {
        var (svc, _) = Build();
        await svc.SetAsync<String?>("explicit-null", null);

        (await svc.TryGetAsync<String>("never-set")).Found.Should().BeFalse();

        var cached = await svc.TryGetAsync<String>("explicit-null");
        cached.Found.Should().BeTrue();
        cached.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetOrAddAsync_CachedNull_ShortCircuitsFactory()
    {
        // L2 缓存了 null（穿透防护）。GetOrAdd 应直接返回，不触发 factory。
        var (svc, _) = Build();
        await svc.SetAsync<String?>("k", null);

        var calls = 0;
        Task<String?> Factory(CancellationToken _) { Interlocked.Increment(ref calls); return Task.FromResult<String?>("never-called"); }

        var result = await svc.GetOrAddAsync("k", Factory);

        result.Should().BeNull();
        calls.Should().Be(0);
    }

    [Fact]
    public async Task GetOrAddAsync_Miss_TriggersFactory_AndCachesResult()
    {
        var (svc, _) = Build();

        var result = await svc.GetOrAddAsync<String>("k", _ => Task.FromResult<String?>("from-factory"));

        result.Should().Be("from-factory");
        (await svc.GetAsync<String>("k")).Should().Be("from-factory");
    }

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
