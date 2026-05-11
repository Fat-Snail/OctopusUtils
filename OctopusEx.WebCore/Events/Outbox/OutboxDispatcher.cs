namespace OctopusEx.WebCore.Events.Outbox;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Outbox 批量派发后台服务。
/// 周期性从 <see cref="IOutboxStore"/> 取出未处理消息，反序列化为 <see cref="IDomainEvent"/> 后通过 <see cref="IEventBus"/> 发布。
///
/// 唤醒策略：
/// - 每轮等 PollInterval 或 IOutboxNotifier 唤醒（arrives first wins）
/// - 业务 EnqueueAsync 触发 notifier，dispatcher 几乎立即拉取，无需等满 PollInterval
///
/// 与 IEventBus 的关系：
/// - 直接 IEventBus.Publish：不保证与业务事务一致；最快、最简单。事务回滚后事件已发出 = bug
/// - Outbox.Enqueue：必须在业务事务内调用。事务提交后才被 dispatcher 取出发布。"至少一次"语义，
///   消费者需幂等（按 EventId 去重）
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOutboxNotifier? _notifier;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly OutboxOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public OutboxDispatcher(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcher> logger,
        IOutboxNotifier? notifier = null,
        OutboxOptions? options = null)
    {
        _serviceProvider = serviceProvider;
        _notifier = notifier;
        _logger = logger;
        _options = options ?? new OutboxOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxDispatcher started: poll={PollInterval}, batch={BatchSize}, notifier={NotifierEnabled}",
            _options.PollInterval, _options.BatchSize, _notifier != null);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                var pending = await store.FetchPendingAsync(_options.BatchSize, _options.MaxAttempts, stoppingToken);
                foreach (var msg in pending)
                {
                    try
                    {
                        var type = Type.GetType(msg.EventType)
                            ?? throw new InvalidOperationException($"Unknown event type: {msg.EventType}");
                        var ev = (IDomainEvent?)JsonSerializer.Deserialize(msg.Payload, type, _jsonOptions)
                            ?? throw new InvalidOperationException("Deserialized to null");
                        await bus.PublishAsync(ev, stoppingToken);
                        await store.MarkProcessedAsync(msg.Id, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Outbox message {Id} dispatch failed", msg.Id);
                        await store.MarkFailedAsync(msg.Id, ex.Message, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxDispatcher iteration failed");
            }

            await WaitForNextRunAsync(stoppingToken);
        }

        _logger.LogInformation("OutboxDispatcher stopped");
    }

    /// <summary>等 PollInterval 或 notifier 信号，arrives first wins。无 notifier 时退化为纯轮询。</summary>
    private async Task WaitForNextRunAsync(CancellationToken stoppingToken)
    {
        if (_notifier == null)
        {
            try { await Task.Delay(_options.PollInterval, stoppingToken); }
            catch (OperationCanceledException) { }
            return;
        }

        using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var notifyTask = _notifier.WaitForNotificationAsync(pollCts.Token);
        var pollTask = Task.Delay(_options.PollInterval, pollCts.Token);

        var winner = await Task.WhenAny(notifyTask, pollTask);
        pollCts.Cancel();
        try { await winner; } catch (OperationCanceledException) { }
    }
}

/// <summary>Outbox 派发配置</summary>
public class OutboxOptions
{
    /// <summary>派发轮询间隔。Notifier 启用时是 fallback 上限；纯 polling 模式下是真实间隔。</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>每次批量取出的消息数</summary>
    public Int32 BatchSize { get; set; } = 100;

    /// <summary>最大重试次数，超过则跳过（保留消息供人工处理）</summary>
    public Int32 MaxAttempts { get; set; } = 5;
}
