namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// Outbox 重试策略。决定失败后下一条消息何时可以被重新派发。
/// </summary>
public enum RetryStrategy
{
    /// <summary>线性回退：间隔 = RetryInterval * AttemptCount</summary>
    Linear = 0,

    /// <summary>指数回退：间隔 = RetryInterval * 2^(AttemptCount - 1)</summary>
    Exponential = 1,

    /// <summary>指数回退 + 抖动（jitter），避免 thundering herd</summary>
    ExponentialWithJitter = 2,
}

/// <summary>Outbox 派发配置</summary>
public class OutboxOptions
{
    /// <summary>派发轮询间隔。Notifier 启用时是 fallback 上限；纯 polling 模式下是真实间隔。</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>每次批量取出的消息数</summary>
    public Int32 BatchSize { get; set; } = 100;

    /// <summary>最大重试次数，超过则跳过（保留消息供人工处理）</summary>
    public Int32 MaxAttempts { get; set; } = 5;

    /// <summary>基础重试间隔。配合 <see cref="RetryStrategy"/> 计算 NextRetry。</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>重试退避策略</summary>
    public RetryStrategy RetryStrategy { get; set; } = RetryStrategy.ExponentialWithJitter;
}
