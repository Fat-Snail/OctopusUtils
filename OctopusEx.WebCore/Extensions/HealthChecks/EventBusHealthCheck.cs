namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Events;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// 事件总线健康检查：检测死信队列是否积压。
/// DeadLetterCount = 0 → Healthy | 1~threshold → Degraded | &gt;threshold → Unhealthy。
/// </summary>
public class EventBusHealthCheck : ICustomHealthCheck
{
    private readonly IDeadLetterStore _deadLetterStore;
    private readonly EventBusHealthCheckOptions _options;

    public EventBusHealthCheck(IDeadLetterStore deadLetterStore, EventBusHealthCheckOptions? options = null)
    {
        _deadLetterStore = deadLetterStore;
        _options = options ?? new EventBusHealthCheckOptions();
    }

    public String Name => "event-bus";
    public String[] Tags => ["eventbus", "ready"];
    public Int32 TimeoutSeconds => 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var deadLetters = await _deadLetterStore.ListAsync(_options.MaxListCount, cancellationToken);
            var count = deadLetters.Count;

            var data = new Dictionary<String, Object>
            {
                { "deadLetterCount", count },
                { "recentDeadLetters", deadLetters.Take(5).Select(d => new { d.EventId, d.EventTypeName, d.HandlerTypeName, d.ErrorMessage }).ToArray() }
            };

            if (count == 0)
                return HealthCheckResult.Healthy("Event bus healthy, no dead letters", data);

            if (count <= _options.DegradedThreshold)
                return HealthCheckResult.Degraded($"Event bus has {count} dead letters (degraded threshold: {_options.DegradedThreshold})", null, data);

            return HealthCheckResult.Unhealthy($"Event bus has {count} dead letters (unhealthy threshold: {_options.UnhealthyThreshold})",
                null, data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Dead letter store unavailable", ex);
        }
    }
}

/// <summary>
/// 事件总线健康检查配置
/// </summary>
public class EventBusHealthCheckOptions
{
    /// <summary>超过此数量标为 Degraded，默认 10</summary>
    public Int32 DegradedThreshold { get; set; } = 10;

    /// <summary>超过此数量标为 Unhealthy，默认 100</summary>
    public Int32 UnhealthyThreshold { get; set; } = 100;

    /// <summary>列表查询最大条数，默认 1000</summary>
    public Int32 MaxListCount { get; set; } = 1000;
}
