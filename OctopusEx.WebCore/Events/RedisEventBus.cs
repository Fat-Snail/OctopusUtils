namespace OctopusEx.WebCore.Events;

using System.Text.Json.Nodes;

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
    private readonly Object _subscriptionsLock = new();
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

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
        => PublishInternalAsync(@event!, @event!.GetType(), cancellationToken);

    public async Task PublishManyAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var ev in events)
            await PublishInternalAsync(ev, ev.GetType(), cancellationToken);
    }

    private Task PublishInternalAsync(Object @event, Type runtimeType, CancellationToken cancellationToken)
    {
        // 单次序列化：把 event 直接作为 envelope 的 payload 字段（JsonNode），
        // 避免"先序列化为字符串再嵌入 envelope"造成的双重转义体积膨胀
        var payloadNode = JsonSerializer.SerializeToNode(@event, runtimeType, _jsonOptions);
        var envelope = new JsonObject
        {
            ["type"] = runtimeType.AssemblyQualifiedName,
            ["payload"] = payloadNode,
        };
        var wire = envelope.ToJsonString(_jsonOptions);
        return _connection.PublishAsync(_channelPrefix + runtimeType.Name, wire, cancellationToken);
    }

    /// <summary>
    /// 订阅指定事件类型的 Redis channel，收到消息后通过本地总线分发到 IEventHandler。
    /// 调用方持有返回的 IDisposable 以单独取消该订阅；DisposeAsync 会撤销所有未释放订阅。
    /// </summary>
    public async Task<IAsyncDisposable> SubscribeAsync<TEvent>(CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var subscription = await _connection.SubscribeAsync(
            _channelPrefix + typeof(TEvent).Name,
            (_, payload) => HandleIncomingAsync(payload, typeof(TEvent), cancellationToken),
            cancellationToken);

        lock (_subscriptionsLock) _subscriptions.Add(subscription);
        return new SubscriptionHandle(this, subscription);
    }

    private async Task HandleIncomingAsync(String payload, Type fallbackType, CancellationToken cancellationToken)
    {
        try
        {
            var envelope = JsonNode.Parse(payload)?.AsObject();
            if (envelope == null) return;

            var typeName = envelope["type"]?.GetValue<String>();
            var type = (typeName != null ? Type.GetType(typeName) : null) ?? fallbackType;
            var payloadNode = envelope["payload"];
            if (payloadNode == null) return;

            var ev = (IDomainEvent?)payloadNode.Deserialize(type, _jsonOptions);
            if (ev != null)
                await _localBus.PublishAsync(ev, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Redis event payload");
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_subscriptionsLock)
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }
        return ValueTask.CompletedTask;
    }

    private sealed class SubscriptionHandle : IAsyncDisposable
    {
        private readonly RedisEventBus _owner;
        private readonly IDisposable _inner;
        private Boolean _disposed;

        public SubscriptionHandle(RedisEventBus owner, IDisposable inner) { _owner = owner; _inner = inner; }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            lock (_owner._subscriptionsLock) _owner._subscriptions.Remove(_inner);
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
