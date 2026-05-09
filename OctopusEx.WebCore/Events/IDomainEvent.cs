namespace OctopusEx.WebCore.Events;

/// <summary>领域事件标记接口。所有自定义事件实现此接口（或继承 <see cref="DomainEventBase"/>）</summary>
public interface IDomainEvent
{
    /// <summary>事件唯一标识，用于去重 / 审计</summary>
    Guid EventId { get; }

    /// <summary>事件发生时间（UTC）</summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>领域事件基类，自动填充 EventId 与 OccurredAt</summary>
public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
