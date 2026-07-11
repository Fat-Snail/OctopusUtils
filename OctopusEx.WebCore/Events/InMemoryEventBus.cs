namespace OctopusEx.WebCore.Events;

using Idempotency;
using Observability;

/// <summary>
/// 进程内事件总线。
/// - 每次发布创建独立 DI scope 解析 IEventHandler&lt;TEvent&gt;，避免 captive dependency
/// - 所有匹配处理器并行执行；单个失败按 maxRetries 指数退避重试，耗尽写 IDeadLetterStore
/// - 一个处理器失败不影响其他处理器
/// - 客户端取消（OperationCanceledException）原样抛出，不计入重试与死信
/// - 支持 [Idempotent] 属性：标在处理器上，按 EventId 去重
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeadLetterStore _deadLetters;
    private readonly IIdempotencyStore? _idempotencyStore;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly EventBusOptions _options;

    public InMemoryEventBus(
        IServiceScopeFactory scopeFactory,
        IDeadLetterStore deadLetters,
        ILogger<InMemoryEventBus> logger,
        IIdempotencyStore? idempotencyStore = null,
        EventBusOptions? options = null)
    {
        _scopeFactory = scopeFactory;
        _deadLetters = deadLetters;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
        _options = options ?? new EventBusOptions();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
        => DispatchAsync(@event, @event!.GetType(), cancellationToken);

    public async Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var ev in events)
            await DispatchAsync(ev, ev.GetType(), cancellationToken);
    }

    private async Task DispatchAsync(IDomainEvent @event, Type eventType, CancellationToken cancellationToken)
    {
        OctopusTelemetry.EventsPublished.Add(1, new KeyValuePair<String, Object?>("event", eventType.Name));

        // 关键：每次发布创建独立 scope，避免 Singleton EventBus 持有 Scoped handlers
        // 造成的 captive dependency（DbContext / HttpClient 等被错误共享）
        using var scope = _scopeFactory.CreateScope();

        var handlerInterface = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handlers = ((IEnumerable<Object>)scope.ServiceProvider.GetServices(handlerInterface)).ToList();

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
        // [Idempotent] 检查：按 EventId 去重
        if (_idempotencyStore != null)
        {
            var idempotentAttr = handler.GetType().GetCustomAttribute<IdempotentAttribute>();
            if (idempotentAttr != null)
            {
                var key = $"event:{@event.EventId}";
                var record = await _idempotencyStore.TryAcquireAsync(new IdempotencyRecord
                {
                    Key = key,
                    EntityType = eventType.FullName,
                    ExpiresAt = DateTimeOffset.UtcNow + idempotentAttr.Ttl,
                }, ct);

                if (record != null)
                {
                    _logger.LogInformation(
                        "Idempotent handler {Handler} skipped duplicate event {EventId} ({EventType})",
                        handler.GetType().Name, @event.EventId, eventType.Name);
                    return;
                }
            }
        }

        var method = HandleMethodCache.GetOrAdd(handler.GetType(),
            t => t.GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync))!);
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
            // 客户端取消不算处理器失败：直接抛出，不计入重试 / 死信
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
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
