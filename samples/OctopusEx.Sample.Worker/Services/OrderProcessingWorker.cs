using OctopusEx.WebCore.Events;

namespace OctopusEx.Sample.Worker;

/// <summary>
/// 演示后台 Worker 服务：定期执行任务并演示事件总线集成。
///
/// 生产部署时可替换为订阅 RedisEventBus（跨进程事件分发）。
/// </summary>
public class OrderProcessingWorker : BackgroundService
{
    private readonly ILogger<OrderProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderProcessingWorker(ILogger<OrderProcessingWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderProcessingWorker started at {Time}", DateTimeOffset.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                // 模拟从消息队列收到订单事件
                var orderEvent = new OrderPlacedEvent(
                    $"ORD-{DateTimeOffset.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                    Math.Round((decimal)(Random.Shared.NextDouble() * 500 + 10), 2));

                await eventBus.PublishAsync(orderEvent, stoppingToken);

                _logger.LogDebug("Published order event: {OrderId}", orderEvent.OrderId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order processing iteration failed");
            }

            // 每 10 秒处理一批
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("OrderProcessingWorker stopped at {Time}", DateTimeOffset.UtcNow);
    }
}
