namespace OctopusEx.WebCore.Coordination;

/// <summary>Redis 原子分布式锁连接抽象，避免 WebCore 强绑定具体 Redis 客户端。</summary>
public interface IRedisDistributedLockConnection
{
    Task<Boolean> TryAcquireAsync(String key, String token, TimeSpan leaseTime, CancellationToken cancellationToken = default);
    Task<Boolean> RenewAsync(String key, String token, TimeSpan leaseTime, CancellationToken cancellationToken = default);
    Task<Boolean> ReleaseAsync(String key, String token, CancellationToken cancellationToken = default);
}

/// <summary>基于 Redis SET NX、Compare-and-Renew 和 Compare-and-Delete 语义的分布式锁。</summary>
public sealed class RedisDistributedLockProvider : IDistributedLockProvider, IAsyncDisposable
{
    private readonly IRedisDistributedLockConnection _connection;
    private readonly String _keyPrefix;
    private readonly HashSet<RedisDistributedLockHandle> _handles = new();
    private readonly Object _sync = new();

    public RedisDistributedLockProvider(IRedisDistributedLockConnection connection, String? keyPrefix = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _keyPrefix = String.IsNullOrWhiteSpace(keyPrefix) ? "octopus:lock:" : keyPrefix!;
    }

    public async ValueTask<IDistributedLockHandle> AcquireAsync(String key, DistributedLockOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        options ??= new DistributedLockOptions();
        options.Validate();
        var redisKey = _keyPrefix + key;
        var token = Guid.NewGuid().ToString("N");
        var deadline = DateTime.UtcNow + options.WaitTime;

        do
        {
            try
            {
                if (await _connection.TryAcquireAsync(redisKey, token, options.LeaseTime, cancellationToken).ConfigureAwait(false))
                {
                    var handle = new RedisDistributedLockHandle(_connection, redisKey, token, options);
                    lock (_sync) _handles.Add(handle);
                    handle.Disposed += RemoveHandle;
                    return handle;
                }
            }
            catch when (options.FailureMode == CoordinationFailureMode.FailOpen)
            {
                return EmptyDistributedLockHandle.Instance;
            }

            if (options.WaitTime == TimeSpan.Zero) return EmptyDistributedLockHandle.Instance;
            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(100, Math.Max(10, options.WaitTime.TotalMilliseconds / 10))), cancellationToken).ConfigureAwait(false);
        } while (DateTime.UtcNow < deadline);

        return EmptyDistributedLockHandle.Instance;
    }

    private void RemoveHandle(RedisDistributedLockHandle handle)
    {
        lock (_sync) _handles.Remove(handle);
    }

    public async ValueTask DisposeAsync()
    {
        RedisDistributedLockHandle[] handles;
        lock (_sync) handles = _handles.ToArray();
        foreach (var handle in handles) await handle.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class RedisDistributedLockHandle : IDistributedLockHandle
    {
        private readonly IRedisDistributedLockConnection _connection;
        private readonly String _key;
        private readonly String _token;
        private readonly DistributedLockOptions _options;
        private Int32 _disposed;

        public RedisDistributedLockHandle(IRedisDistributedLockConnection connection, String key, String token, DistributedLockOptions options)
        { _connection = connection; _key = key; _token = token; _options = options; if (options.AutoRenew) _ = RenewLoopAsync(); }

        public event Action<RedisDistributedLockHandle>? Disposed;
        public Boolean Acquired => Volatile.Read(ref _disposed) == 0;
        public Boolean LeaseLost { get; private set; }
        public String Token => _token;

        public async ValueTask<Boolean> RenewAsync(CancellationToken cancellationToken = default)
        {
            if (!Acquired || LeaseLost) return false;
            try { var renewed = await _connection.RenewAsync(_key, _token, _options.LeaseTime, cancellationToken).ConfigureAwait(false); if (!renewed) LeaseLost = true; return renewed; }
            catch { LeaseLost = true; if (_options.FailureMode == CoordinationFailureMode.FailClosed) throw; return false; }
        }

        private async Task RenewLoopAsync()
        {
            while (Acquired && !LeaseLost)
            {
                try { await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(100, _options.LeaseTime.TotalMilliseconds / 3))).ConfigureAwait(false); if (Acquired) await RenewAsync().ConfigureAwait(false); }
                catch { LeaseLost = true; }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            try { if (!LeaseLost) await _connection.ReleaseAsync(_key, _token).ConfigureAwait(false); }
            finally { Disposed?.Invoke(this); }
        }
    }
}
