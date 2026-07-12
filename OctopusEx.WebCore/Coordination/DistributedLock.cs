namespace OctopusEx.WebCore.Coordination;

/// <summary>分布式协调故障策略。</summary>
public enum CoordinationFailureMode
{
    /// <summary>协调后端不可用时拒绝进入临界区。</summary>
    FailClosed,
    /// <summary>协调后端不可用时允许继续执行。</summary>
    FailOpen
}

/// <summary>分布式锁配置。</summary>
public sealed class DistributedLockOptions
{
    /// <summary>锁租约时间。</summary>
    public TimeSpan LeaseTime { get; set; } = TimeSpan.FromSeconds(30);
    /// <summary>等待锁的最长时间。</summary>
    public TimeSpan WaitTime { get; set; } = TimeSpan.Zero;
    /// <summary>是否自动续租。</summary>
    public Boolean AutoRenew { get; set; } = true;
    /// <summary>协调后端异常时的行为。</summary>
    public CoordinationFailureMode FailureMode { get; set; } = CoordinationFailureMode.FailClosed;

    internal void Validate()
    {
        if (LeaseTime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(LeaseTime));
        if (WaitTime < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(WaitTime));
    }
}

/// <summary>分布式锁提供者。</summary>
public interface IDistributedLockProvider
{
    /// <summary>尝试获取指定 key 的锁。</summary>
    ValueTask<IDistributedLockHandle> AcquireAsync(
        String key,
        DistributedLockOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>分布式锁句柄。</summary>
public interface IDistributedLockHandle : IAsyncDisposable
{
    /// <summary>是否成功获取锁。</summary>
    Boolean Acquired { get; }
    /// <summary>锁是否已因续租失败而丢失。</summary>
    Boolean LeaseLost { get; }
    /// <summary>当前租约的持有者令牌。</summary>
    String? Token { get; }
    /// <summary>主动续租。</summary>
    ValueTask<Boolean> RenewAsync(CancellationToken cancellationToken = default);
}

/// <summary>锁未获取时使用的空句柄。</summary>
internal sealed class EmptyDistributedLockHandle : IDistributedLockHandle
{
    public static readonly EmptyDistributedLockHandle Instance = new();
    public Boolean Acquired => false;
    public Boolean LeaseLost => false;
    public String? Token => null;
    public ValueTask<Boolean> RenewAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
