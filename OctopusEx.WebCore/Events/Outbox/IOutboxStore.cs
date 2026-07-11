namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// Outbox 持久化记录。事务内与业务数据一起落库；后台 dispatcher 取出并发布。
///
/// 字段为 init-only，构造后只能由 IOutboxStore 实现内部修改 ProcessedAt / AttemptCount / LastError / NextRetry。
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public String EventType { get; init; } = "";
    public String Payload { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // 这四个字段在 dispatch 过程中由 IOutboxStore 实现更新。
    // 设为 public set 以兼容外部存储实现（如 EFOutboxStore 在独立 assembly）。
    public DateTimeOffset? ProcessedAt { get; set; }
    public Int32 AttemptCount { get; set; }
    public String? LastError { get; set; }
    public DateTimeOffset? NextRetry { get; set; }
}

/// <summary>
/// Outbox 存储抽象。生产实现通常是 EF Core 表（与业务实体同事务），
/// 测试用 InMemoryOutboxStore。
/// </summary>
public interface IOutboxStore
{
    /// <summary>把待发送的事件落库（业务事务内调用）。</summary>
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>取出未处理且 AttemptCount &lt; maxAttempts 的批次（按时间升序）。</summary>
    /// <param name="batchSize">单次最多取出的消息数</param>
    /// <param name="maxAttempts">已达此重试次数的消息会被跳过（仍保留在存储中供人工处理）</param>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(Int32 batchSize, Int32 maxAttempts, CancellationToken cancellationToken = default);

    /// <summary>标记成功处理。</summary>
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>标记失败（自增 AttemptCount，记录错误，计算 NextRetry，留待下次重试）。</summary>
    Task MarkFailedAsync(Guid messageId, String error, CancellationToken cancellationToken = default);

    /// <summary>标记失败并指定重试策略。由实现计算 NextRetry 时间。</summary>
    Task MarkFailedAsync(Guid messageId, String error, RetryStrategy retryStrategy, TimeSpan retryInterval, CancellationToken cancellationToken = default);
}

/// <summary>
/// 进程内 Outbox 存储。调用 EnqueueAsync 时若注册了 IOutboxNotifier，会立即唤醒 dispatcher。
/// 用于单元测试与开发环境。
/// </summary>
public class InMemoryOutboxStore : IOutboxStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, OutboxMessage> _store = new();
    private readonly IOutboxNotifier? _notifier;

    public InMemoryOutboxStore(IOutboxNotifier? notifier = null) => _notifier = notifier;

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _store[message.Id] = message;
        _notifier?.Notify();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(Int32 batchSize, Int32 maxAttempts, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<OutboxMessage> list = _store.Values
            .Where(m => m.ProcessedAt == null && m.AttemptCount < maxAttempts && (!m.NextRetry.HasValue || m.NextRetry.Value <= now))
            .OrderBy(m => m.NextRetry ?? m.CreatedAt)
            .Take(batchSize)
            .ToList();
        return Task.FromResult(list);
    }

    public Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(messageId, out var msg)) msg.ProcessedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid messageId, String error, RetryStrategy retryStrategy, TimeSpan retryInterval, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(messageId, out var msg))
        {
            msg.AttemptCount++;
            msg.LastError = error;
            msg.NextRetry = CalculateNextRetry(msg.AttemptCount, retryStrategy, retryInterval);
        }
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid messageId, String error, CancellationToken cancellationToken = default)
        => MarkFailedAsync(messageId, error, RetryStrategy.ExponentialWithJitter, TimeSpan.FromSeconds(30), cancellationToken);

    private static DateTimeOffset CalculateNextRetry(Int32 attemptCount, RetryStrategy strategy, TimeSpan baseInterval) =>
        strategy switch
        {
            RetryStrategy.Linear => DateTimeOffset.UtcNow + baseInterval * attemptCount,
            RetryStrategy.Exponential => DateTimeOffset.UtcNow + baseInterval * Math.Pow(2, attemptCount - 1),
            RetryStrategy.ExponentialWithJitter => DateTimeOffset.UtcNow + Random.Shared.NextDouble() * baseInterval * Math.Pow(2, attemptCount - 1) + baseInterval,
            _ => DateTimeOffset.UtcNow + baseInterval * Math.Pow(2, attemptCount - 1),
        };

    /// <summary>测试辅助：列出所有消息（含已处理）。</summary>
    public IReadOnlyList<OutboxMessage> Snapshot() => _store.Values.ToList();
}
