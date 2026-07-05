namespace OctopusEx.WebCore.Idempotency;

/// <summary>
/// 标在 <see cref="IEventHandler{T}"/> 实现类上，声明该处理器需要按 EventId 去重。
/// 重复事件（相同 EventId）将被跳过，不会重复处理。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class IdempotentAttribute : Attribute
{
    /// <summary>幂等键的过期时间（秒）。默认 86400（24 小时）。</summary>
    public Int32 TtlSeconds { get; set; } = 86400;

    /// <summary>幂等键的过期时间（便捷属性）。</summary>
    public TimeSpan Ttl => TimeSpan.FromSeconds(TtlSeconds);
}
