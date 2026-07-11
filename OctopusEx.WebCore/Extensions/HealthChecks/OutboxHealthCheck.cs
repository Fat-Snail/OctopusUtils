namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Events.Outbox;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Outbox 健康检查：检测待处理消息积压。
/// PendingCount ≤ DegradedThreshold → Healthy | &lt; UnhealthyThreshold → Degraded | ≥ UnhealthyThreshold → Unhealthy。
/// </summary>
public class OutboxHealthCheck : ICustomHealthCheck
{
    private readonly IOutboxStore _outboxStore;
    private readonly OutboxHealthCheckOptions _options;

    public OutboxHealthCheck(IOutboxStore outboxStore, OutboxHealthCheckOptions? options = null)
    {
        _outboxStore = outboxStore;
        _options = options ?? new OutboxHealthCheckOptions();
    }

    public String Name => "outbox";
    public String[] Tags => ["outbox", "ready"];
    public Int32 TimeoutSeconds => 10;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // FetchPendingAsync 会跳过超过最大重试次数的消息；积压指"仍可重试的数量"
            var pending = await _outboxStore.FetchPendingAsync(_options.MaxQueryCount, Int32.MaxValue, cancellationToken);
            var pendingCount = pending.Count;

            var data = new Dictionary<String, Object>
            {
                { "pendingMessageCount", pendingCount },
                { "oldestPending", pending.OrderBy(m => m.CreatedAt).FirstOrDefault()?.CreatedAt.ToString("O") ?? "n/a" },
                { "recentErrors", pending.Where(m => m.LastError != null).Take(3).Select(m => new { m.Id, m.EventType, m.AttemptCount, m.LastError }).ToArray() }
            };

            if (pendingCount <= _options.DegradedThreshold)
                return HealthCheckResult.Healthy($"Outbox healthy, {pendingCount} pending", data);

            if (pendingCount < _options.UnhealthyThreshold)
                return HealthCheckResult.Degraded($"Outbox has {pendingCount} pending messages (degraded threshold: {_options.DegradedThreshold})", null, data);

            return HealthCheckResult.Unhealthy($"Outbox has {pendingCount} pending messages (unhealthy threshold: {_options.UnhealthyThreshold})", null, data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Outbox store unavailable", ex);
        }
    }
}

/// <summary>
/// Outbox 健康检查配置
/// </summary>
public class OutboxHealthCheckOptions
{
    /// <summary>超过此数量标为 Degraded，默认 100</summary>
    public Int32 DegradedThreshold { get; set; } = 100;

    /// <summary>超过此数量标为 Unhealthy，默认 500</summary>
    public Int32 UnhealthyThreshold { get; set; } = 500;

    /// <summary>查询最大数量，默认 1000</summary>
    public Int32 MaxQueryCount { get; set; } = 1000;
}
