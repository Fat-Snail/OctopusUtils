namespace OctopusEx.Sample.WebApi.Coordination;

using OctopusEx.WebCore.Coordination;

/// <summary>演示两个并发任务竞争同一把租约锁。</summary>
public sealed class CoordinationDemoService
{
    private readonly IDistributedLockProvider lockProvider;

    public CoordinationDemoService(IDistributedLockProvider lockProvider)
    {
        this.lockProvider = lockProvider;
    }

    public async Task<CoordinationDemoResult> RunAsync(CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var contenders = await Task.WhenAll(
            RunContenderAsync("worker-a", TimeSpan.Zero, cancellationToken),
            RunContenderAsync("worker-b", TimeSpan.FromMilliseconds(10), cancellationToken));

        return new CoordinationDemoResult
        {
            LockKey = "sample:coordination-demo",
            StartedAt = startedAt,
            FinishedAt = DateTimeOffset.UtcNow,
            Contenders = contenders
        };
    }

    private async Task<CoordinationContenderResult> RunContenderAsync(
        String name,
        TimeSpan startDelay,
        CancellationToken cancellationToken)
    {
        await Task.Delay(startDelay, cancellationToken);
        var startedAt = DateTimeOffset.UtcNow;
        await using var handle = await lockProvider.AcquireAsync(
            "sample:coordination-demo",
            new DistributedLockOptions
            {
                LeaseTime = TimeSpan.FromSeconds(2),
                WaitTime = TimeSpan.Zero,
                AutoRenew = true
            },
            cancellationToken);

        if (!handle.Acquired)
        {
            return new CoordinationContenderResult
            {
                Name = name,
                Acquired = false,
                StartedAt = startedAt,
                FinishedAt = DateTimeOffset.UtcNow,
                Message = "未获取到锁：另一个任务正在临界区执行"
            };
        }

        await Task.Delay(150, cancellationToken);
        return new CoordinationContenderResult
        {
            Name = name,
            Acquired = true,
            LeaseLost = handle.LeaseLost,
            Token = handle.Token,
            StartedAt = startedAt,
            FinishedAt = DateTimeOffset.UtcNow,
            Message = "成功进入临界区并完成释放"
        };
    }
}

public sealed class CoordinationDemoResult
{
    public String LockKey { get; init; } = String.Empty;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
    public IReadOnlyList<CoordinationContenderResult> Contenders { get; init; } = [];
}

public sealed class CoordinationContenderResult
{
    public String Name { get; init; } = String.Empty;
    public Boolean Acquired { get; init; }
    public Boolean LeaseLost { get; init; }
    public String? Token { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; init; }
    public String Message { get; init; } = String.Empty;
}
