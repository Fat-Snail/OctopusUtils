namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check for cache services (Redis, Memory Cache, etc.)
/// </summary>
public class CacheHealthCheck : ICustomHealthCheck
{
    private readonly string _cacheType;
    private readonly string _connectionString;

    public CacheHealthCheck(string cacheType = "Redis", string connectionString = "localhost:6379")
    {
        _cacheType = cacheType;
        _connectionString = connectionString;
    }

    public string Name => "cache";
    public string[] Tags => ["cache", "live"];
    public int TimeoutSeconds => 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate cache connection and operations
            await Task.Delay(Random.Shared.Next(20, 100), cancellationToken);

            // Simulate cache issues (3% failure rate)
            if ( Random.Shared.NextDouble() < 0.03 )
            {
                return HealthCheckResult.Degraded(
                    $"{_cacheType} cache performance degraded",
                    null,
                    new Dictionary<string, object>
                    {
                        {"cacheType", _cacheType},
                        {"connectionString", _connectionString},
                        {"latencyMs", Random.Shared.Next(500, 2000)},
                        {"failureReason", "High latency"}
                    });
            }

            return HealthCheckResult.Healthy($"{_cacheType} cache service operational", new Dictionary<string, object>
            {
                {"cacheType", _cacheType},
                {"connectionString", _connectionString},
                {"latencyMs", Random.Shared.Next(1, 50)},
                {"hitRate", Random.Shared.Next(85, 99)},
                {"memoryUsageMb", Random.Shared.Next(100, 500)}
            });
        }
        catch ( Exception ex )
        {
            return HealthCheckResult.Unhealthy($"{_cacheType} cache service unavailable", ex, new Dictionary<string, object>
            {
                {"cacheType", _cacheType},
                {"connectionString", _connectionString}
            });
        }
    }
}
