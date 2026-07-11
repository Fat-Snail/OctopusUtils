namespace OctopusEx.WebCore.Idempotency;

/// <summary>
/// 幂等键记录。用于 HTTP 请求和事件消费的去重。
/// </summary>
public sealed class IdempotencyRecord
{
    /// <summary>幂等键（如请求头 Idempotency-Key 或 EventId）</summary>
    public String Key { get; init; } = "";

    /// <summary>关联的实体类型（optional metadata）</summary>
    public String? EntityType { get; init; }

    /// <summary>记录创建时间</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>过期时间。超过此时间后记录可被清理。</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>HTTP 响应体或事件处理结果的序列化缓存（可选，用于快速返回重复请求的结果）</summary>
    public String? ResultCache { get; set; }

    /// <summary>响应状态码（仅 HTTP 场景）</summary>
    public Int32? StatusCode { get; set; }

    public Boolean IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
}

/// <summary>
/// 幂等去重存储抽象。支持 HTTP 请求（Idempotency-Key）与事件消费（EventId）两种场景。
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// 尝试获取幂等锁。如果键已存在且未过期，返回已有的记录（表示重复）。
    /// 如果键不存在，插入新记录并返回 null（表示首次请求）。
    /// </summary>
    /// <returns>已有记录（重复请求）或 null（首次请求）</returns>
    Task<IdempotencyRecord?> TryAcquireAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);

    /// <summary>记录处理结果（可选：缓存响应体 / 状态码）。</summary>
    Task SetResultAsync(String key, Int32 statusCode, String? resultBody, CancellationToken cancellationToken = default);

    /// <summary>获取已缓存的结果（用于快速返回重复请求）。</summary>
    Task<IdempotencyRecord?> GetAsync(String key, CancellationToken cancellationToken = default);

    /// <summary>清理过期记录，返回删除数量。</summary>
    Task<Int32> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>幂等存储配置</summary>
public class IdempotencyOptions
{
    /// <summary>记录默认过期时间。默认 24 小时。</summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>过期记录清理间隔。默认 1 小时。</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>用于 HTTP 请求的幂等键请求头名称。</summary>
    public String HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>是否启用 HTTP 请求幂等中间件。</summary>
    public Boolean EnableHttpMiddleware { get; set; } = true;

    /// <summary>仅对以下 HTTP 方法启用幂等（默认 POST/PUT/PATCH/DELETE）。</summary>
    public HashSet<String> ApplicableMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE",
    };
}
