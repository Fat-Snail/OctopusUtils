namespace OctopusEx.WebCore.Events;

/// <summary>
/// 事件处理器抽象。每个事件可有 0..N 个处理器；同一类型的多个处理器并行执行。
/// 实现类会被 <see cref="Extensions.EventBusExtensions.AddSimpleEventBus"/> 自动扫描注册。
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
