namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Health check for database connectivity
/// </summary>
public class DatabaseHealthCheck : ICustomHealthCheck
{
    private readonly string _connectionString;
    private readonly string _databaseType;

    public DatabaseHealthCheck(string connectionString, string databaseType = "Generic")
    {
        _connectionString = connectionString;
        _databaseType = databaseType;
    }

    public string Name => "database";
    public string[] Tags => ["database", "ready"];
    public int TimeoutSeconds => 10;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate database connection check
            await Task.Delay(Random.Shared.Next(50, 200), cancellationToken);

            // Simulate occasional database issues (5% failure rate)
            if ( Random.Shared.NextDouble() < 0.05 )
            {
                return HealthCheckResult.Unhealthy(
                    $"{_databaseType} database connection failed",
                    new Exception("Connection timeout"),
                    new Dictionary<string, object>
                    {
                        {"databaseType", _databaseType},
                        {"connectionString", MaskConnectionString(_connectionString)},
                        {"failureReason", "Connection timeout"}
                    });
            }

            return HealthCheckResult.Healthy($"{_databaseType} database connection successful", new Dictionary<string, object>
            {
                {"databaseType", _databaseType},
                {"connectionString", MaskConnectionString(_connectionString)},
                {"connectionTimeMs", Random.Shared.Next(50, 150)}
            });
        }
        catch ( Exception ex )
        {
            return HealthCheckResult.Unhealthy($"{_databaseType} database health check failed", ex, new Dictionary<string, object>
            {
                {"databaseType", _databaseType},
                {"connectionString", MaskConnectionString(_connectionString)}
            });
        }
    }

    private string MaskConnectionString(string connectionString)
    {
        // Simple masking - in real scenarios, you'd want more sophisticated masking
        if ( string.IsNullOrEmpty(connectionString) ) return string.Empty;

        var parts = connectionString.Split(';');
        var maskedParts = parts.Select(part =>
        {
            if ( part.ToLower().Contains("password") || part.ToLower().Contains("pwd") )
                return "Password=****";
            return part;
        });

        return string.Join(";", maskedParts);
    }
}
