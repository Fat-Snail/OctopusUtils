namespace OctopusEx.WebCore.Events;

using Observability;

/// <summary>
/// 进程内事件总线。
/// - 通过 IServiceProvider 解析 IEventHandler&lt;TEvent&gt;，所有匹配处理器并行执行
/// - 单个处理器失败按 maxRetries 指数退避重试；耗尽后写入 IDeadLetterStore
/// - 一个处理器失败不影响其他处理器
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeadLetterStore _deadLetters;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly EventBusOptions _options;

    public InMemoryEventBus(
        IServiceProvider serviceProvider,
        IDeadLetterStore deadLetters,
        ILogger<InMemoryEventBus> logger,
        EventBusOptions? options = null)
    {
        _serviceProvider = serviceProvider;
        _deadLetters = deadLetters;
        _logger = logger;
        _options = options ?? new EventBusOptions();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
        => DispatchAsync(@event, typeof(TEvent), cancellationToken);

    public async Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var ev in events)
            await DispatchAsync(ev, ev.GetType(), cancellationToken);
    }

    private async Task DispatchAsync(IDomainEvent @event, Type eventType, CancellationToken cancellationToken)
    {
        OctopusTelemetry.EventsPublished.Add(1, new KeyValuePair<String, Object?>("event", eventType.Name));

        var handlerInterface = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = ((IEnumerable<Object>)_serviceProvider.GetServices(handlerInterface)).ToList();

        if (handlers.Count == 0)
        {
            _logger.LogDebug("No handler for event {EventType}", eventType.Name);
            return;
        }

        var tasks = handlers.Select(h => InvokeWithRetryAsync(h, @event, eventType, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task InvokeWithRetryAsync(Object handler, IDomainEvent @event, Type eventType, CancellationToken ct)
    {
        var method = handler.GetType().GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync))!;
        var attempt = 0;
        Exception? last = null;

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                var task = (Task)method.Invoke(handler, new Object[] { @event, ct })!;
                await task;
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                attempt++;
                _logger.LogWarning(ex,
                    "Event handler {Handler} failed on attempt {Attempt}/{Max} for event {Event}",
                    handler.GetType().Name, attempt, _options.MaxRetries + 1, eventType.Name);
                if (attempt > _options.MaxRetries) break;
                var delay = TimeSpan.FromMilliseconds(_options.RetryBaseDelayMs * Math.Pow(2, attempt - 1));
                try { await Task.Delay(delay, ct); }
                catch (OperationCanceledException) { return; }
            }
        }

        OctopusTelemetry.EventHandlerFailures.Add(1,
            new KeyValuePair<String, Object?>("handler", handler.GetType().Name),
            new KeyValuePair<String, Object?>("event", eventType.Name));

        await _deadLetters.RecordAsync(new DeadLetter(
            @event.EventId,
            eventType.FullName ?? eventType.Name,
            handler.GetType().FullName ?? handler.GetType().Name,
            last?.Message ?? "unknown",
            DateTimeOffset.UtcNow,
            attempt), ct);
    }
}

/// <summary>事件总线配置</summary>
public class EventBusOptions
{
    /// <summary>最大重试次数（不含首次）。默认 2，即总共最多 3 次。</summary>
    public Int32 MaxRetries { get; set; } = 2;

    /// <summary>首次重试延迟基数（毫秒），后续按 2^n 指数退避。</summary>
    public Int32 RetryBaseDelayMs { get; set; } = 100;
}
