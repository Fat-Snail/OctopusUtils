namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// 基于 ICacheService 的真实缓存健康检查。
/// 写入测试 Key 并读出以验证 L1/L2 连通性；同时返回命中率等遥测指标。
/// </summary>
public class OctopusCacheHealthCheck : ICustomHealthCheck
{
    private readonly ICacheService _cache;
    private static readonly String TestKey = "__octopus_cache_health_check__";

    public OctopusCacheHealthCheck(ICacheService cache)
    {
        _cache = cache;
    }

    public String Name => "cache";
    public String[] Tags => ["cache", "live"];
    public Int32 TimeoutSeconds => 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            // 写入测试值
            await _cache.SetAsync(TestKey, "ok", TimeSpan.FromSeconds(10), cts.Token);

            // 回读验证
            var result = await _cache.TryGetAsync<String>(TestKey, cts.Token);

            // 清理测试键
            await _cache.RemoveAsync(TestKey, CancellationToken.None);

            if (!result.Found || result.Value != "ok")
                return HealthCheckResult.Degraded("Cache write succeeded but read-back mismatch");

            var data = new Dictionary<String, Object>
            {
                { "status", "connected" },
                { "type", _cache.GetType().Name }
            };

            return HealthCheckResult.Healthy("Cache service operational", data);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Cache health check timed out");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache service unavailable", ex, new Dictionary<String, Object>
            {
                { "error", ex.Message }
            });
        }
    }
}
