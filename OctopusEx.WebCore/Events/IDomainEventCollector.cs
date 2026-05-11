namespace OctopusEx.WebCore.Events;

/// <summary>
/// 领域事件收集器。Scope 内累积事件，由 UnitOfWork 在 SaveChanges 之后批量发布，
/// 实现"事务提交后再触发副作用"的模式（避免事务失败但副作用已执行）。
/// </summary>
public interface IDomainEventCollector
{
    /// <summary>累积事件，等待 Flush 触发分发</summary>
    void Raise(IDomainEvent @event);

    /// <summary>取出并清空累积的事件（线程安全）</summary>
    IReadOnlyList<IDomainEvent> Drain();
}

/// <summary>
/// 默认进程内收集器，作为 Scoped 服务注册。
/// 线程安全：Raise 与 Drain 可被并发调用（典型场景：handler 内部派生新事件）。
/// </summary>
public class DomainEventCollector : IDomainEventCollector
{
    private readonly Object _lock = new();
    private List<IDomainEvent> _events = new();

    public void Raise(IDomainEvent @event)
    {
        lock (_lock) _events.Add(@event);
    }

    public IReadOnlyList<IDomainEvent> Drain()
    {
        lock (_lock)
        {
            // 整体换 list，O(1) 清空 + 旧 list 已脱离锁直接作为返回值（调用方独占）
            var snapshot = _events;
            _events = new List<IDomainEvent>();
            return snapshot;
        }
    }
}
