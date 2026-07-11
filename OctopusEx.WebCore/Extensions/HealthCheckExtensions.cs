namespace OctopusEx.WebCore.Extensions;

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
    /// Adds common health checks to the service.
    /// Auto-detects registered IHealthCheck / ICustomHealthCheck implementations and wires them to the correct tags.
    /// </summary>
    public static TBuilder AddCommonHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var healthChecks = builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        // 检查并添加已注册的健康检查
        if ( builder.Services.Any(sd => sd.ServiceType == typeof(DatabaseHealthCheck)) )
        {
            healthChecks.AddCheck<DatabaseHealthCheck>("database", tags: ["database", "ready"]);
        }

        if ( builder.Services.Any(sd => sd.ServiceType == typeof(ExternalApiHealthCheck)) )
        {
            healthChecks.AddCheck<ExternalApiHealthCheck>("external-api", tags: ["external", "ready"]);
        }

        if ( builder.Services.Any(sd => sd.ServiceType == typeof(CacheHealthCheck)) )
        {
            healthChecks.AddCheck<CacheHealthCheck>("cache", tags: ["cache", "live"]);
        }

        // v1.5.5 — 新的模块健康检查
        if ( builder.Services.Any(sd => sd.ServiceType == typeof(OctopusCacheHealthCheck)) )
        {
            healthChecks.AddCheck<OctopusCacheHealthCheck>("cache", tags: ["cache", "live"]);
        }

        if ( builder.Services.Any(sd => sd.ServiceType == typeof(EventBusHealthCheck)) )
        {
            healthChecks.AddCheck<EventBusHealthCheck>("event-bus", tags: ["eventbus", "ready"]);
        }

        if ( builder.Services.Any(sd => sd.ServiceType == typeof(OutboxHealthCheck)) )
        {
            healthChecks.AddCheck<OutboxHealthCheck>("outbox", tags: ["outbox", "ready"]);
        }

        if ( builder.Services.Any(sd => sd.ServiceType == typeof(TenantHealthCheck)) )
        {
            healthChecks.AddCheck<TenantHealthCheck>("tenant", tags: ["tenant", "ready"]);
        }

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

    // ---- v1.5.5 新增模块健康检查注册 ----

    /// <summary>
    /// 注册基于 ICacheService 的真实缓存健康检查（替代模拟版 CacheHealthCheck）。
    /// 需要先注册 ICacheService（通过 AddSimpleCache / AddMultiLevelCache）。
    /// </summary>
    public static TBuilder AddOctopusCacheHealthCheck<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton<OctopusCacheHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<OctopusCacheHealthCheck>("cache", tags: ["cache", "live"]);
        return builder;
    }

    /// <summary>
    /// 注册事件总线健康检查（监控死信队列积压量）。
    /// </summary>
    public static TBuilder AddEventBusHealthCheck<TBuilder>(
        this TBuilder builder,
        Action<EventBusHealthCheckOptions>? configure = null) where TBuilder : IHostApplicationBuilder
    {
        var options = new EventBusHealthCheckOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<EventBusHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<EventBusHealthCheck>("event-bus", tags: ["eventbus", "ready"]);
        return builder;
    }

    /// <summary>
    /// 注册 Outbox 健康检查（监控待处理消息积压量）。
    /// </summary>
    public static TBuilder AddOutboxHealthCheck<TBuilder>(
        this TBuilder builder,
        Action<OutboxHealthCheckOptions>? configure = null) where TBuilder : IHostApplicationBuilder
    {
        var options = new OutboxHealthCheckOptions();
        configure?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<OutboxHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<OutboxHealthCheck>("outbox", tags: ["outbox", "ready"]);
        return builder;
    }

    /// <summary>
    /// 注册多租户健康检查（验证 ICurrentTenant / ITenantConnectionResolver 注册状态）。
    /// </summary>
    public static TBuilder AddTenantHealthCheck<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddSingleton<TenantHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<TenantHealthCheck>("tenant", tags: ["tenant", "ready"]);
        return builder;
    }
}
