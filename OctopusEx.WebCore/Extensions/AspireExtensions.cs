using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OctopusEx.WebCore.Extensions;

using OpenTelemetry;
using OpenTelemetry.Metrics;

public static class AspireExtensions
{
    public static IHostApplicationBuilder AddAspireOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.ConfigureOpenTelemetry();
        return builder;
    }

    private static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(AspireExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    serviceInstanceId: Environment.MachineName)
            )
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                        if ( !String.IsNullOrWhiteSpace(otlpEndpoint) )
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }
                    });

            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => { options.RecordException = true; })
                    .AddHttpClientInstrumentation(options => { options.RecordException = true; })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        // 启用 SQL 语句显示
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                    })
                    .AddOtlpExporter(options =>
                    {
                        // Check if OTLP endpoint is configured (Aspire will set this automatically)
                        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

                        if ( !String.IsNullOrWhiteSpace(otlpEndpoint) )
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        }
                    });
            });


        // var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
        // Console.WriteLine($"[ServiceDefaults] OTLP exporter enabled: {useOtlpExporter}, endpoint: {builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]}");
        //
        // if (useOtlpExporter)
        // {
        //     builder.Services.AddOpenTelemetry().UseOtlpExporter();
        // }
        
        // 关键：为日志配置相同的资源构建器
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.AddOtlpExporter();// Aspire will automatically configure the endpoint
        });

        return builder;
    }
}
