using OctopusEx.WebCore.Events;

namespace OctopusEx.Sample.Worker;

/// <summary>
/// 演示需要后台处理的事件。
/// </summary>
public class OrderPlacedEvent : DomainEventBase
{
    public string OrderId { get; }
    public decimal Amount { get; }

    public OrderPlacedEvent(string orderId, decimal amount)
    {
        OrderId = orderId;
        Amount = amount;
    }
}

/// <summary>
/// 订单处理事件处理器。
/// 在 Worker 进程中处理 WebApi 发布的事件。
/// </summary>
public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing order: OrderId={OrderId}, Amount={Amount:C}",
            @event.OrderId, @event.Amount);

        // 模拟处理：库存扣减、通知发送等
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation(
            "Order processed successfully: OrderId={OrderId}",
            @event.OrderId);
    }
}
