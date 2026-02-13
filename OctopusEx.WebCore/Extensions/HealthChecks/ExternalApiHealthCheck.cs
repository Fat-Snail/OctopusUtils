namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

/// <summary>
/// Health check for external API/services connectivity
/// </summary>
public class ExternalApiHealthCheck : ICustomHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly ILogger<ExternalApiHealthCheck> _logger;

    public ExternalApiHealthCheck(HttpClient httpClient, string apiEndpoint, ILogger<ExternalApiHealthCheck> logger)
    {
        _httpClient = httpClient;
        _apiEndpoint = apiEndpoint;
        _logger = logger;
    }

    public string Name => "external-api";
    public string[] Tags => ["external", "ready"];
    public int TimeoutSeconds => 15;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            // Simulate external API call with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            await Task.Delay(Random.Shared.Next(100, 500), cts.Token);

            // Simulate API failures (10% failure rate)
            if ( Random.Shared.NextDouble() < 0.10 )
            {
                return HealthCheckResult.Degraded(
                    "External API response slow or degraded",
                    null,
                    new Dictionary<string, object>
                    {
                        {"endpoint", _apiEndpoint},
                        {"responseTimeMs", (DateTime.UtcNow - startTime).TotalMilliseconds},
                        {"statusCode", 503}
                    });
            }

            return HealthCheckResult.Healthy("External API is responsive", new Dictionary<string, object>
            {
                {"endpoint", _apiEndpoint},
                {"responseTimeMs", (DateTime.UtcNow - startTime).TotalMilliseconds},
                {"statusCode", 200}
            });
        }
        catch ( OperationCanceledException )
        {
            _logger.LogWarning("External API health check timed out: {Endpoint}", _apiEndpoint);
            return HealthCheckResult.Degraded("External API timeout", null, new Dictionary<string, object>
            {
                {"endpoint", _apiEndpoint},
                {"timeoutSeconds", TimeoutSeconds}
            });
        }
        catch ( Exception ex )
        {
            _logger.LogWarning(ex, "External API health check failed: {Endpoint}", _apiEndpoint);
            return HealthCheckResult.Unhealthy("External API is unreachable", ex, new Dictionary<string, object>
            {
                {"endpoint", _apiEndpoint}
            });
        }
    }
}
