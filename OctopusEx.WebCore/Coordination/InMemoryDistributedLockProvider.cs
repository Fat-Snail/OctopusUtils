namespace OctopusEx.WebCore.Coordination;

using System.Collections.Concurrent;

/// <summary>
/// 进程内分布式锁实现。适用于单实例部署和测试，不提供跨进程协调保证。
/// </summary>
public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider, IDisposable
{
    private sealed class LockState
    {
        public Object SyncRoot { get; } = new();
        public String? Token { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private readonly ConcurrentDictionary<String, LockState> states = new(StringComparer.Ordinal);
    private readonly String keyPrefix;
    private Boolean disposed;

    public InMemoryDistributedLockProvider(String? keyPrefix = null)
    {
        this.keyPrefix = String.IsNullOrWhiteSpace(keyPrefix) ? "octopus" : keyPrefix.Trim();
    }

    public async ValueTask<IDistributedLockHandle> AcquireAsync(
        String key,
        DistributedLockOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        options ??= new DistributedLockOptions();
        options.Validate();

        var state = states.GetOrAdd($"{keyPrefix}:{key}", _ => new LockState());
        var deadline = DateTimeOffset.UtcNow + options.WaitTime;
        var token = Guid.NewGuid().ToString("N");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (state.SyncRoot)
            {
                var now = DateTimeOffset.UtcNow;
                if (state.Token == null || state.ExpiresAt <= now)
                {
                    state.Token = token;
                    state.ExpiresAt = now + options.LeaseTime;
                    var handle = new InMemoryDistributedLockHandle(state, token, options);
                    handle.StartAutoRenew();
                    return handle;
                }
            }

            if (DateTimeOffset.UtcNow >= deadline)
                return EmptyDistributedLockHandle.Instance;

            var delay = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(25) > deadline
                ? deadline - DateTimeOffset.UtcNow
                : TimeSpan.FromMilliseconds(25);
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        disposed = true;
        states.Clear();
    }

    private sealed class InMemoryDistributedLockHandle : IDistributedLockHandle
    {
        private readonly LockState state;
        private readonly String token;
        private readonly DistributedLockOptions options;
        private CancellationTokenSource? renewCancellation;
        private Task? renewTask;
        private Int32 disposed;

        public InMemoryDistributedLockHandle(LockState state, String token, DistributedLockOptions options)
        {
            this.state = state;
            this.token = token;
            this.options = options;
        }

        public Boolean Acquired => Volatile.Read(ref disposed) == 0 && !LeaseLost;
        public Boolean LeaseLost { get; private set; }
        public String Token => token;

        public void StartAutoRenew()
        {
            if (!options.AutoRenew) return;
            renewCancellation = new CancellationTokenSource();
            renewTask = RenewLoopAsync(renewCancellation.Token);
        }

        public ValueTask<Boolean> RenewAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (state.SyncRoot)
            {
                if (Volatile.Read(ref disposed) != 0 || state.Token != token || state.ExpiresAt <= DateTimeOffset.UtcNow)
                {
                    LeaseLost = true;
                    return ValueTask.FromResult(false);
                }

                state.ExpiresAt = DateTimeOffset.UtcNow + options.LeaseTime;
                return ValueTask.FromResult(true);
            }
        }

        private async Task RenewLoopAsync(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromMilliseconds(Math.Max(100, options.LeaseTime.TotalMilliseconds / 3));
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                    if (!await RenewAsync(cancellationToken).ConfigureAwait(false)) break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            if (renewCancellation != null)
            {
                await renewCancellation.CancelAsync().ConfigureAwait(false);
                if (renewTask != null)
                {
                    try { await renewTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                }
                renewCancellation.Dispose();
            }

            lock (state.SyncRoot)
            {
                if (state.Token == token) state.Token = null;
            }
        }
    }
}
