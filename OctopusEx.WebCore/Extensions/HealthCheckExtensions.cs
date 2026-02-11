namespace Octopus.Extensions;

using HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for adding health checks to services
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds common health checks to the service
    /// </summary>
    public static TBuilder AddCommonHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["database", "ready"])
            .AddCheck<ExternalApiHealthCheck>("external-api", tags: ["external", "ready"])
            .AddCheck<CacheHealthCheck>("cache", tags: ["cache", "live"]);

        return builder;
    }

    /// <summary>
    /// Adds a database health check with custom configuration
    /// </summary>
    public static TBuilder AddDatabaseHealthCheck<TBuilder>(
        this TBuilder builder,
        string name = "database",
        string connectionString = "Server=localhost;Database=testdb;Trusted_Connection=true;",
        string databaseType = "SQLServer") where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton(new DatabaseHealthCheck(connectionString, databaseType));
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(name, tags: ["database", "ready"]);

        return builder;
    }

    /// <summary>
    /// Adds an external API health check
    /// </summary>
    public static TBuilder AddExternalApiHealthCheck<TBuilder>(
        this TBuilder builder,
        string name = "external-api",
        string apiEndpoint = "https://api.example.com/health") where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHttpClient<ExternalApiHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<ExternalApiHealthCheck>(name, tags: ["external", "ready"]);

        return builder;
    }

    /// <summary>
    /// Adds a cache health check
    /// </summary>
    public static TBuilder AddCacheHealthCheck<TBuilder>(
        this TBuilder builder,
        string name = "cache",
        string cacheType = "Redis",
        string connectionString = "localhost:6379") where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton(new CacheHealthCheck(cacheType, connectionString));
        builder.Services.AddHealthChecks()
            .AddCheck<CacheHealthCheck>(name, tags: ["cache", "live"]);

        return builder;
    }

    /// <summary>
    /// Adds a custom business logic health check
    /// </summary>
    public static TBuilder AddBusinessLogicHealthCheck<TBuilder>(
        this TBuilder builder,
        string name,
        Func<CancellationToken, Task<HealthCheckResult>> checkFunction,
        string[]? tags = null) where TBuilder : IHostApplicationBuilder
    {
        tags ??= ["business", "ready"];

        builder.Services.AddHealthChecks()
            .AddAsyncCheck(name, checkFunction, tags);

        return builder;
    }

    /// <summary>
    /// Maps comprehensive health check endpoints
    /// </summary>
    public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
    {
        // Ready endpoint - all checks must pass
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        // Live endpoint - only liveness checks
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        // Full health check
        app.MapHealthChecks("/health/full");

        // Enhanced health endpoint with detailed status
        app.MapGet("/health", async (Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();

            return Microsoft.AspNetCore.Http.Results.Ok(new
            {
                Service = app.Environment.ApplicationName,
                Status = report.Status.ToString(),
                TotalDurationMs = report.TotalDuration.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
                Entries = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Description = e.Value.Description,
                    DurationMs = e.Value.Duration.TotalMilliseconds,
                    Tags = e.Value.Tags,
                    Data = e.Value.Data
                }).ToArray()
            });
        });

        return app;
    }

    /// <summary>
    /// Gets the health check configuration section
    /// </summary>
    public static IConfigurationSection GetHealthCheckConfiguration(this IHostApplicationBuilder builder)
    {
        return builder.Configuration.GetSection("HealthCheck");
    }
}
