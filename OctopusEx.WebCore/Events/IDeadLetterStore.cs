namespace OctopusEx.WebCore.Events;

/// <summary>处理失败的死信记录</summary>
public sealed record DeadLetter(
    Guid EventId,
    String EventTypeName,
    String HandlerTypeName,
    String ErrorMessage,
    DateTimeOffset FailedAt,
    Int32 AttemptCount);

/// <summary>
/// 死信存储抽象。事件处理重试耗尽仍失败后写入此处，方便人工补偿或离线分析。
/// 默认 InMemoryDeadLetterStore；生产可替换为 EF / Redis / RabbitMQ DLX。
/// </summary>
public interface IDeadLetterStore
{
    Task RecordAsync(DeadLetter letter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeadLetter>> ListAsync(Int32 take = 100, CancellationToken cancellationToken = default);
}

/// <summary>进程内死信存储，FIFO 截断到 capacity</summary>
public class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<DeadLetter> _store = new();
    private readonly Int32 _capacity;

    public InMemoryDeadLetterStore(Int32 capacity = 1000) => _capacity = capacity;

    public Task RecordAsync(DeadLetter letter, CancellationToken cancellationToken = default)
    {
        _store.Enqueue(letter);
        while (_store.Count > _capacity) _store.TryDequeue(out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeadLetter>> ListAsync(Int32 take = 100, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DeadLetter>>(_store.Reverse().Take(take).ToList());
}
