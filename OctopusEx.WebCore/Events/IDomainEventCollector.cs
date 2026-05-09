namespace OctopusEx.WebCore.Events;

/// <summary>
/// 领域事件收集器。Scope 内累积事件，由 UnitOfWork 在 SaveChanges 之后批量发布，
/// 实现"事务提交后再触发副作用"的模式（避免事务失败但副作用已执行）。
/// </summary>
public interface IDomainEventCollector
{
    /// <summary>累积事件，等待 Flush 触发分发</summary>
    void Raise(IDomainEvent @event);

    /// <summary>取出并清空累积的事件（线程不安全，调用方应在 SaveChanges 之后单线程使用）</summary>
    IReadOnlyList<IDomainEvent> Drain();
}

/// <summary>默认进程内收集器，作为 Scoped 服务注册</summary>
public class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = new();

    public void Raise(IDomainEvent @event) => _events.Add(@event);

    public IReadOnlyList<IDomainEvent> Drain()
    {
        var snapshot = _events.ToList();
        _events.Clear();
        return snapshot;
    }
}
