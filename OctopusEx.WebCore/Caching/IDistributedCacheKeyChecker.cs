namespace OctopusEx.WebCore.Caching;

/// <summary>
/// 高效"键是否存在"探针抽象。
///
/// 背景：<see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> 没有原生 EXISTS API，
/// 默认 ExistsAsync 通过 GetAsync 拉取整个 payload 后判 null —— 大值场景明显浪费带宽。
///
/// 用户对接 Redis 后可实现本接口直接调用 RedisDB.KeyExists，避免拉全量。
/// 未注册时 <see cref="DistributedCacheService"/> 自动 fallback 到 GetAsync 路径。
///
/// Redis 实现示例：
/// <code>
/// public class RedisKeyChecker(IConnectionMultiplexer mux) : IDistributedCacheKeyChecker
/// {
///     public Task&lt;bool&gt; ExistsAsync(string key, CancellationToken ct) =&gt;
///         mux.GetDatabase().KeyExistsAsync(key);
/// }
/// </code>
/// </summary>
public interface IDistributedCacheKeyChecker
{
    Task<Boolean> ExistsAsync(String fullKey, CancellationToken cancellationToken = default);
}
