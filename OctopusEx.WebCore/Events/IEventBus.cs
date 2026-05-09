namespace OctopusEx.WebCore.Events;

/// <summary>
/// 事件总线抽象。默认 InMemoryEventBus 进程内调度；用户可替换为 Redis Pub/Sub 等跨进程实现。
/// </summary>
public interface IEventBus
{
    /// <summary>发布单个事件，触发所有订阅的 IEventHandler。</summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;

    /// <summary>批量发布。同类型事件可被聚合处理；语义上等同于多次 Publish。</summary>
    Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default);
}
