namespace OctopusEx.WebCore.Caching;

/// <summary>
/// 缓存全局配置
/// </summary>
public class CacheOptions
{
    /// <summary>默认过期时间</summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>TTL 随机抖动比例（0 ~ 1，雪崩防护）。0.1 表示实际 TTL 为 [DefaultTtl, DefaultTtl * 1.1] 区间随机值。</summary>
    public Double TtlJitterPercent { get; set; } = 0.1;

    /// <summary>是否缓存 null 值（穿透防护）</summary>
    public Boolean CacheNullValues { get; set; } = true;

    /// <summary>null 值缓存时长（穿透防护，短时缓存）</summary>
    public TimeSpan NullValueTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>键前缀（多应用隔离）</summary>
    public String KeyPrefix { get; set; } = "";

    internal String BuildKey(String key) => String.IsNullOrEmpty(KeyPrefix) ? key : $"{KeyPrefix}{key}";

    internal TimeSpan ApplyJitter(TimeSpan ttl)
    {
        if (TtlJitterPercent <= 0) return ttl;
        var jitter = Random.Shared.NextDouble() * TtlJitterPercent;
        return TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * (1 + jitter));
    }
}
