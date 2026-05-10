namespace OctopusEx.WebCore.Events.Outbox;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Outbox 批量派发后台服务。
/// 周期性从 IOutboxStore 取出未处理消息，反序列化为 IDomainEvent 后通过 IEventBus 发布。
/// 失败消息自增 AttemptCount，超过 MaxAttempts 后保留在 outbox 但不再处理（人工介入）。
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly OutboxOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger, OutboxOptions? options = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options ?? new OutboxOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started: poll={PollInterval}, batch={BatchSize}", _options.PollInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

                var pending = await store.FetchPendingAsync(_options.BatchSize, stoppingToken);
                foreach (var msg in pending)
                {
                    if (msg.AttemptCount >= _options.MaxAttempts)
                    {
                        _logger.LogWarning("Outbox message {Id} exceeded MaxAttempts ({Max}), skipping", msg.Id, _options.MaxAttempts);
                        continue;
                    }

                    try
                    {
                        var type = Type.GetType(msg.EventType)
                            ?? throw new InvalidOperationException($"Unknown event type: {msg.EventType}");
                        var ev = (IDomainEvent?)JsonSerializer.Deserialize(msg.Payload, type, _jsonOptions)
                            ?? throw new InvalidOperationException("Deserialized to null");
                        await bus.PublishAsync(ev, stoppingToken);
                        await store.MarkProcessedAsync(msg.Id, stoppingToken);
                    }
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

            try { await Task.Delay(_options.PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _logger.LogInformation("OutboxDispatcher stopped");
    }
}

/// <summary>Outbox 派发配置</summary>
public class OutboxOptions
{
    /// <summary>派发轮询间隔，默认 1 秒</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>每次批量取出的消息数</summary>
    public Int32 BatchSize { get; set; } = 100;

    /// <summary>最大重试次数，超过则跳过（保留消息供人工处理）</summary>
    public Int32 MaxAttempts { get; set; } = 5;
}
