namespace OctopusEx.WebCore.Idempotency;

/// <summary>
/// Redis 幂等存储实现。使用 Redis SETNX 和 TTL 机制。
/// </summary>
public class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IRedisIdempotencyConnection _connection;
    private readonly IdempotencyOptions _options;
    private readonly String _prefix;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public RedisIdempotencyStore(
        IRedisIdempotencyConnection connection,
        IdempotencyOptions options,
        String keyPrefix = "octopus:idempotency:")
    {
        _connection = connection;
        _options = options;
        _prefix = keyPrefix;
    }

    public async Task<IdempotencyRecord?> TryAcquireAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var key = $"{_prefix}{record.Key}";
        var ttl = (Int64)(record.ExpiresAt - record.CreatedAt).TotalSeconds;

        // 使用 SET NX（SET key value NX EX ttl）原子操作
        var value = JsonSerializer.Serialize(new
        {
            record.EntityType,
            CreatedAt = record.CreatedAt,
            ExpiresAt = record.ExpiresAt,
        }, _jsonOptions);

        var acquired = await _connection.SetIfNotExistsAsync(key, value, TimeSpan.FromSeconds(ttl), cancellationToken);

        if (!acquired)
        {
            // 键已存在，读取已有值
            var existing = await _connection.GetAsync(key, cancellationToken);
            if (existing != null)
            {
                var doc = JsonDocument.Parse(existing);
                return new IdempotencyRecord
                {
                    Key = record.Key,
                    EntityType = doc.RootElement.TryGetProperty("EntityType", out var et) ? et.GetString() : null,
                    CreatedAt = doc.RootElement.TryGetProperty("CreatedAt", out var ca) ? ca.GetDateTimeOffset() : DateTimeOffset.UtcNow,
                    ExpiresAt = doc.RootElement.TryGetProperty("ExpiresAt", out var ea) ? ea.GetDateTimeOffset() : DateTimeOffset.UtcNow.AddHours(1),
                };
            }
        }

        return null; // 首次请求（成功获取）
    }

    public async Task SetResultAsync(String key, Int32 statusCode, String? resultBody, CancellationToken cancellationToken = default)
    {
        var redisKey = $"{_prefix}{key}";

        // 先读取现有值
        var existing = await _connection.GetAsync(redisKey, cancellationToken);
        if (existing == null) return;

        var doc = JsonDocument.Parse(existing);
        var updated = new Dictionary<String, JsonElement?>();

        foreach (var prop in doc.RootElement.EnumerateObject())
            updated[prop.Name] = prop.Value;

        updated["StatusCode"] = JsonSerializer.SerializeToElement(statusCode);
        updated["ResultCache"] = resultBody != null ? JsonSerializer.SerializeToElement(resultBody) : default;

        var ttl = await _connection.GetTtlAsync(redisKey, cancellationToken);
        if (ttl > 0)
        {
            await _connection.SetAsync(redisKey, JsonSerializer.Serialize(updated, _jsonOptions), TimeSpan.FromSeconds(ttl), cancellationToken);
        }
    }

    public async Task<IdempotencyRecord?> GetAsync(String key, CancellationToken cancellationToken = default)
    {
        var redisKey = $"{_prefix}{key}";
        var value = await _connection.GetAsync(redisKey, cancellationToken);
        if (value == null) return null;

        var doc = JsonDocument.Parse(value);
        var root = doc.RootElement;

        if (root.TryGetProperty("ExpiresAt", out var ea) && ea.GetDateTimeOffset() <= DateTimeOffset.UtcNow)
        {
            // 过期了，删除
            await _connection.DeleteAsync(redisKey, cancellationToken);
            return null;
        }

        return new IdempotencyRecord
        {
            Key = key,
            EntityType = root.TryGetProperty("EntityType", out var et) ? et.GetString() : null,
            CreatedAt = root.TryGetProperty("CreatedAt", out var ca) ? ca.GetDateTimeOffset() : DateTimeOffset.UtcNow,
            ExpiresAt = root.TryGetProperty("ExpiresAt", out var ea2) ? ea2.GetDateTimeOffset() : DateTimeOffset.UtcNow,
            StatusCode = root.TryGetProperty("StatusCode", out var sc) ? sc.GetInt32() : null,
            ResultCache = root.TryGetProperty("ResultCache", out var rc) ? rc.GetString() : null,
        };
    }

    public Task<Int32> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        // Redis 通过 TTL 自动清理过期键，无需手动清理。
        // 如果需要主动清理，可使用 SCAN + DEL（但 KEYS 会阻塞）。
        return Task.FromResult(0);
    }
}

/// <summary>
/// Redis 连接抽象层。避免强依赖 StackExchange.Redis。
/// </summary>
public interface IRedisIdempotencyConnection
{
    Task<String?> GetAsync(String key, CancellationToken cancellationToken = default);
    Task<Boolean> SetIfNotExistsAsync(String key, String value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task SetAsync(String key, String value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<Int64> GetTtlAsync(String key, CancellationToken cancellationToken = default);
    Task DeleteAsync(String key, CancellationToken cancellationToken = default);
}
