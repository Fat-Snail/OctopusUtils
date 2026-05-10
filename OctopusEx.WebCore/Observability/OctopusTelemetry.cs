namespace OctopusEx.WebCore.Observability;

using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// OctopusEx 全局可观测性资源：单一 ActivitySource 与 Meter，方便 OpenTelemetry 一次性订阅。
///
/// OTel 配置：
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(OctopusTelemetry.SourceName))
///     .WithMetrics(m => m.AddMeter(OctopusTelemetry.SourceName));
/// </code>
/// </summary>
public static class OctopusTelemetry
{
    /// <summary>所有 OctopusEx 模块共用的 source/meter 名</summary>
    public const String SourceName = "OctopusEx";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    // ---- Cache 指标 ----
    /// <summary>缓存命中计数（标签：layer = L1|L2|MISS）</summary>
    public static readonly Counter<Int64> CacheHits = Meter.CreateCounter<Int64>(
        "octopus.cache.hits", description: "Cache hit/miss count by layer");

    /// <summary>缓存 GetOrAdd factory 执行次数</summary>
    public static readonly Counter<Int64> CacheFactoryExecutions = Meter.CreateCounter<Int64>(
        "octopus.cache.factory_executions", description: "Number of factory invocations on cache miss");

    // ---- AI 指标 ----
    /// <summary>AI 调用次数（标签：operation = ask|stream|structured，status = ok|error）</summary>
    public static readonly Counter<Int64> AiInvocations = Meter.CreateCounter<Int64>(
        "octopus.ai.invocations", description: "Number of IOctopusChatService invocations");

    /// <summary>AI 调用延迟（毫秒）</summary>
    public static readonly Histogram<Double> AiLatencyMs = Meter.CreateHistogram<Double>(
        "octopus.ai.latency_ms", unit: "ms", description: "Latency of AI calls");

    // ---- 事件总线指标 ----
    /// <summary>事件发布数（按 EventType 标签）</summary>
    public static readonly Counter<Int64> EventsPublished = Meter.CreateCounter<Int64>(
        "octopus.events.published", description: "Domain events published by type");

    /// <summary>事件处理失败计数（按 HandlerType 标签）</summary>
    public static readonly Counter<Int64> EventHandlerFailures = Meter.CreateCounter<Int64>(
        "octopus.events.handler_failures", description: "Event handler failures (after retries)");
}
