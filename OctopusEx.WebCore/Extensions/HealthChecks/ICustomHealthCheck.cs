namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Base interface for custom health checks with additional metadata
/// </summary>
public interface ICustomHealthCheck : IHealthCheck
{
    /// <summary>
    /// Gets the name of the health check
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the tags associated with this health check
    /// </summary>
    string[] Tags { get; }

    /// <summary>
    /// Gets the timeout for this health check in seconds
    /// </summary>
    int TimeoutSeconds { get; }
}
