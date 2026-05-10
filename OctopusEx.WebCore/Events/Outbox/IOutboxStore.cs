namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// Outbox 消息持久化记录。事务提交时与业务数据一起落库；后台 dispatcher 定期取出并发布。
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public String EventType { get; set; } = "";
    public String Payload { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public Int32 AttemptCount { get; set; }
    public String? LastError { get; set; }
}

/// <summary>
/// Outbox 存储抽象。生产实现通常是 EF Core 表（与业务实体同事务），
/// 测试用 InMemoryOutboxStore。
/// </summary>
public interface IOutboxStore
{
    /// <summary>把待发送的事件落库（业务事务内调用）。</summary>
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>取出未处理的批次（按时间升序）。</summary>
    Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(Int32 batchSize, CancellationToken cancellationToken = default);

    /// <summary>标记成功处理。</summary>
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>标记失败（自增 AttemptCount，记录错误，留待下次重试）。</summary>
    Task MarkFailedAsync(Guid messageId, String error, CancellationToken cancellationToken = default);
}

/// <summary>进程内 Outbox 存储，用于单元测试与开发环境。</summary>
public class InMemoryOutboxStore : IOutboxStore
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, OutboxMessage> _store = new();

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _store[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(Int32 batchSize, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OutboxMessage> list = _store.Values
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToList();
        return Task.FromResult(list);
    }

    public Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(messageId, out var msg)) msg.ProcessedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid messageId, String error, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(messageId, out var msg))
        {
            msg.AttemptCount++;
            msg.LastError = error;
        }
        return Task.CompletedTask;
    }

    /// <summary>测试辅助：列出所有消息（含已处理）。</summary>
    public IReadOnlyList<OutboxMessage> Snapshot() => _store.Values.ToList();
}
