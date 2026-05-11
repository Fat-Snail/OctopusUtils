namespace OctopusEx.WebCore.Events;

/// <summary>
/// Redis 连接抽象，避免 OctopusEx.WebCore 强绑定 StackExchange.Redis。
/// 用户实现此接口（通常一行包装 IConnectionMultiplexer）后注入 RedisEventBus。
/// </summary>
public interface IRedisEventBusConnection
{
    /// <summary>发布消息到指定 channel。</summary>
    Task PublishAsync(String channel, String payload, CancellationToken cancellationToken = default);

    /// <summary>订阅 channel。messageHandler 在收到消息时回调。返回的 IDisposable 取消订阅。</summary>
    Task<IDisposable> SubscribeAsync(String channel, Func<String, String, Task> messageHandler, CancellationToken cancellationToken = default);
}

/// <summary>
/// 跨进程 Redis Pub/Sub 事件总线。
/// 工作流程：
/// 1. PublishAsync 把事件序列化为 JSON 推到 Redis channel
/// 2. 订阅方收到消息后反序列化并通过本地 InMemoryEventBus 分发到 IEventHandler
/// 3. 与本地总线组合：Local → 本进程，Redis → 跨进程。
/// 调用方需先通过 DI 注册 IRedisEventBusConnection。
/// </summary>
public class RedisEventBus : IEventBus, IAsyncDisposable
{
    private const String DefaultChannelPrefix = "octopus:events:";

    private readonly IRedisEventBusConnection _connection;
    private readonly InMemoryEventBus _localBus;
    private readonly ILogger<RedisEventBus> _logger;
    private readonly String _channelPrefix;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RedisEventBus(
        IRedisEventBusConnection connection,
        InMemoryEventBus localBus,
        ILogger<RedisEventBus> logger,
        String channelPrefix = DefaultChannelPrefix)
    {
        _connection = connection;
        _localBus = localBus;
        _logger = logger;
        _channelPrefix = channelPrefix;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        // 用运行时类型而非编译期 TEvent，避免调用方持有基类引用时派生类字段被序列化丢失
        var runtimeType = @event!.GetType();
        var envelope = new EventEnvelope(runtimeType.AssemblyQualifiedName!,
            JsonSerializer.Serialize(@event, runtimeType, _jsonOptions));
        var payload = JsonSerializer.Serialize(envelope, _jsonOptions);
        await _connection.PublishAsync(_channelPrefix + runtimeType.Name, payload, cancellationToken);
    }

    public async Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var ev in events)
        {
            var type = ev.GetType();
            var envelope = new EventEnvelope(type.AssemblyQualifiedName!, JsonSerializer.Serialize(ev, type, _jsonOptions));
            var payload = JsonSerializer.Serialize(envelope, _jsonOptions);
            await _connection.PublishAsync(_channelPrefix + type.Name, payload, cancellationToken);
        }
    }

    /// <summary>
    /// 订阅指定事件类型的 Redis channel，收到消息后通过本地总线分发到 IEventHandler。
    /// 调用方在应用启动时为感兴趣的事件类型逐个调用本方法。
    /// </summary>
    public async Task SubscribeAsync<TEvent>(CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        var subscription = await _connection.SubscribeAsync(
            _channelPrefix + typeof(TEvent).Name,
            async (_, payload) =>
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<EventEnvelope>(payload, _jsonOptions);
                    if (envelope == null) return;

                    var type = Type.GetType(envelope.TypeName) ?? typeof(TEvent);
                    var ev = (IDomainEvent?)JsonSerializer.Deserialize(envelope.Payload, type, _jsonOptions);
                    if (ev != null)
                        await _localBus.PublishAsync(ev, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to handle Redis event payload");
                }
            },
            cancellationToken);

        _subscriptions.Add(subscription);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed record EventEnvelope(String TypeName, String Payload);
}
