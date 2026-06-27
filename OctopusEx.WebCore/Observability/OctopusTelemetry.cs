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
///
/// 多实例 / 多租户场景需要独立命名时，注入 <see cref="IOctopusTelemetry"/> 而非直接使用静态成员。
/// </summary>
public static class OctopusTelemetry
{
    /// <summary>所有 OctopusEx 模块共用的 source/meter 名</summary>
    public const String SourceName = "OctopusEx";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName);

    // ---- Cache 指标 ----
    public static readonly Counter<Int64> CacheHits = Meter.CreateCounter<Int64>(
        "octopus.cache.hits", description: "Cache hit/miss count by layer");

    public static readonly Counter<Int64> CacheFactoryExecutions = Meter.CreateCounter<Int64>(
        "octopus.cache.factory_executions", description: "Number of factory invocations on cache miss");

    // ---- AI 指标 ----
    public static readonly Counter<Int64> AiInvocations = Meter.CreateCounter<Int64>(
        "octopus.ai.invocations", description: "Number of IOctopusChatService invocations");

    public static readonly Histogram<Double> AiLatencyMs = Meter.CreateHistogram<Double>(
        "octopus.ai.latency_ms", unit: "ms", description: "Latency of AI calls");

    // ---- 事件总线指标 ----
    public static readonly Counter<Int64> EventsPublished = Meter.CreateCounter<Int64>(
        "octopus.events.published", description: "Domain events published by type");

    public static readonly Counter<Int64> EventHandlerFailures = Meter.CreateCounter<Int64>(
        "octopus.events.handler_failures", description: "Event handler failures (after retries)");

    /// <summary>用静态实例包装为 IOctopusTelemetry，方便代码统一通过接口访问。</summary>
    public static readonly IOctopusTelemetry Default = new StaticTelemetryAdapter();

    private sealed class StaticTelemetryAdapter : IOctopusTelemetry
    {
        public ActivitySource ActivitySource => OctopusTelemetry.ActivitySource;
        public Meter Meter => OctopusTelemetry.Meter;
        public Counter<Int64> CacheHits => OctopusTelemetry.CacheHits;
        public Counter<Int64> CacheFactoryExecutions => OctopusTelemetry.CacheFactoryExecutions;
        public Counter<Int64> AiInvocations => OctopusTelemetry.AiInvocations;
        public Histogram<Double> AiLatencyMs => OctopusTelemetry.AiLatencyMs;
        public Counter<Int64> EventsPublished => OctopusTelemetry.EventsPublished;
        public Counter<Int64> EventHandlerFailures => OctopusTelemetry.EventHandlerFailures;
    }
}

/// <summary>
/// OctopusEx 可观测性资源的接口形态，用于多 host / 多命名空间隔离场景。
/// 默认实现 <see cref="OctopusTelemetry.Default"/> 委托到静态成员；用户可注入 <see cref="NamedOctopusTelemetry"/> 配置不同 SourceName。
/// </summary>
public interface IOctopusTelemetry
{
    ActivitySource ActivitySource { get; }
    Meter Meter { get; }
    Counter<Int64> CacheHits { get; }
    Counter<Int64> CacheFactoryExecutions { get; }
    Counter<Int64> AiInvocations { get; }
    Histogram<Double> AiLatencyMs { get; }
    Counter<Int64> EventsPublished { get; }
    Counter<Int64> EventHandlerFailures { get; }
}

/// <summary>命名隔离实现：按用户传入的前缀创建独立的 ActivitySource + Meter 与一组同名指标。</summary>
public sealed class NamedOctopusTelemetry : IOctopusTelemetry, IDisposable
{
    public ActivitySource ActivitySource { get; }
    public Meter Meter { get; }
    public Counter<Int64> CacheHits { get; }
    public Counter<Int64> CacheFactoryExecutions { get; }
    public Counter<Int64> AiInvocations { get; }
    public Histogram<Double> AiLatencyMs { get; }
    public Counter<Int64> EventsPublished { get; }
    public Counter<Int64> EventHandlerFailures { get; }

    public NamedOctopusTelemetry(String sourceName)
    {
        var prefix = sourceName.ToLowerInvariant();
        ActivitySource = new ActivitySource(sourceName);
        Meter = new Meter(sourceName);
        CacheHits = Meter.CreateCounter<Int64>($"{prefix}.cache.hits");
        CacheFactoryExecutions = Meter.CreateCounter<Int64>($"{prefix}.cache.factory_executions");
        AiInvocations = Meter.CreateCounter<Int64>($"{prefix}.ai.invocations");
        AiLatencyMs = Meter.CreateHistogram<Double>($"{prefix}.ai.latency_ms", unit: "ms");
        EventsPublished = Meter.CreateCounter<Int64>($"{prefix}.events.published");
        EventHandlerFailures = Meter.CreateCounter<Int64>($"{prefix}.events.handler_failures");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}
