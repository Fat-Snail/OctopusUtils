namespace OctopusEx.Aspire;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// .NET Aspire 风格的 ServiceDefaults 一站式配置。
/// 一行接入：
/// <code>
/// builder.AddOctopusServiceDefaults();
/// // ...
/// app.MapOctopusDefaultEndpoints();
/// </code>
///
/// 启用：
/// - OpenTelemetry：traces / metrics / logs，自动通过 OTLP exporter 发送
/// - 服务发现 + HTTP 弹性（Microsoft.Extensions.ServiceDiscovery + Http.Resilience）
/// - 默认健康检查：/health（活）+ /alive（存活探针）
/// </summary>
public static class AspireServiceDefaults
{
    public static IHostApplicationBuilder AddOctopusServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();
        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlp = !String.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        if (useOtlp)
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        return builder;
    }

    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });
        return builder;
    }

    /// <summary>
    /// 映射默认端点。仅在 Development 环境暴露 /health（含详细信息）；/alive 任何环境都开放（K8s 存活探针）。
    /// </summary>
    public static WebApplication MapOctopusDefaultEndpoints(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapHealthChecks("/health");

        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
        });

        return app;
    }
}
