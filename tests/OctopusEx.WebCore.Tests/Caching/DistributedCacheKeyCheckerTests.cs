namespace OctopusEx.WebCore.Tests.Caching;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using OctopusEx.WebCore.Caching;

public class DistributedCacheKeyCheckerTests
{
    [Fact]
    public async Task ExistsAsync_WithKeyChecker_BypassesGetAsync()
    {
        var raw = new TracingDistributedCache();
        var checker = new StubKeyChecker(exists: true);
        var svc = new DistributedCacheService(raw, new CacheOptions(), NullLogger<DistributedCacheService>.Instance, checker);

        (await svc.ExistsAsync("k")).Should().BeTrue();
        raw.GetCalls.Should().Be(0, "因为 checker 已直接返回结果，不应再拉 payload");
        checker.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ExistsAsync_WithoutKeyChecker_FallsBackToGetAsync()
    {
        var raw = new TracingDistributedCache();
        await raw.SetAsync("k", new Byte[] { 1, 2, 3 }, new DistributedCacheEntryOptions());
        var svc = new DistributedCacheService(raw, new CacheOptions(), NullLogger<DistributedCacheService>.Instance);

        (await svc.ExistsAsync("k")).Should().BeTrue();
        raw.GetCalls.Should().Be(1);
    }

    private sealed class StubKeyChecker : IDistributedCacheKeyChecker
    {
        private readonly Boolean _exists;
        public Int32 Calls { get; private set; }
        public StubKeyChecker(Boolean exists) => _exists = exists;
        public Task<Boolean> ExistsAsync(String fullKey, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(_exists);
        }
    }

    private sealed class TracingDistributedCache : IDistributedCache
    {
        private readonly Dictionary<String, Byte[]> _store = new();
        public Int32 GetCalls { get; private set; }

        public Byte[]? Get(String key) { GetCalls++; return _store.TryGetValue(key, out var v) ? v : null; }
        public Task<Byte[]?> GetAsync(String key, CancellationToken token = default) => Task.FromResult(Get(key));
        public void Set(String key, Byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;
        public Task SetAsync(String key, Byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { Set(key, value, options); return Task.CompletedTask; }
        public void Refresh(String key) { }
        public Task RefreshAsync(String key, CancellationToken token = default) => Task.CompletedTask;
        public void Remove(String key) => _store.Remove(key);
        public Task RemoveAsync(String key, CancellationToken token = default) { Remove(key); return Task.CompletedTask; }
    }
}
