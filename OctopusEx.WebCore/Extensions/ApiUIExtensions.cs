using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace OctopusEx.WebCore.Extensions;

/// <summary>
/// API UI 扩展方法，用于配置 Swagger UI 和 Scalar UI
/// </summary>
public static class ApiUIExtensions
{
    /// <summary>
    /// 添加 Swagger UI 服务
    /// </summary>
    public static IServiceCollection AddSwaggerUIServices(this IServiceCollection services, 
        string title = "API", 
        string version = "v1", 
        string description = "ASP.NET Core Web API")
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new OpenApiInfo 
            { 
                Title = title, 
                Version = version,
                Description = description
            });
        });
        
        return services;
    }

    /// <summary>
    /// 只配置 Swagger UI
    /// </summary>
    public static WebApplication UseOnlySwaggerUI(this WebApplication app, 
        string swaggerVersion = "v1", 
        string routePrefix = "swagger")
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"{app.Environment.ApplicationName} {swaggerVersion}");
                c.RoutePrefix = routePrefix;
            });
        }

        return app;
    }

    /// <summary>
    /// 只配置 Scalar UI
    /// </summary>
    public static WebApplication UseOnlyScalarUI(this WebApplication app, 
        string title = "API Documentation",
        ScalarTheme theme = ScalarTheme.Default,
        ScalarLayout layout = ScalarLayout.Modern)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle(title)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithTheme(theme)
                    .WithDarkModeToggle(true)
                    .WithLayout(layout)
                    .WithCustomCss(@"
                        .scalar-api-reference {
                            --scalar-font-family: 'Segoe UI', system-ui, sans-serif;
                        }
                        .scalar-header {
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        }
                    ");
            });
        }

        return app;
    }

    /// <summary>
    /// 同时配置 Swagger UI 和 Scalar UI
    /// </summary>
    public static WebApplication UseBothApiUIs(this WebApplication app, 
        string title = "API Documentation",
        string swaggerVersion = "v1",
        ScalarTheme scalarTheme = ScalarTheme.Default,
        ScalarLayout scalarLayout = ScalarLayout.Modern)
    {
        if (app.Environment.IsDevelopment())
        {
            // 配置 Scalar UI
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle(title)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithTheme(scalarTheme)
                    .WithDarkModeToggle(true)
                    .WithLayout(scalarLayout)
                    .WithCustomCss(@"
                        .scalar-api-reference {
                            --scalar-font-family: 'Segoe UI', system-ui, sans-serif;
                        }
                        .scalar-header {
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        }
                    ");
            });
            
            // 配置 Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"{app.Environment.ApplicationName} {swaggerVersion}");
                c.RoutePrefix = "swagger";
            });
        }

        return app;
    }

    /// <summary>
    /// 添加 API 文档导航页面
    /// </summary>
    public static WebApplication AddApiDocumentationNavigation(this WebApplication app, 
        bool enableSwagger = true, 
        bool enableScalar = true)
    {
        var endpoints = new Dictionary<string, string>();
        
        if (enableSwagger)
            endpoints.Add("swagger", "/swagger");
            
        if (enableScalar)
            endpoints.Add("scalar", "/scalar");
            
        endpoints.Add("openapi", "/openapi/v1.json");

        app.MapGet("/", () => new { 
            message = "Welcome to API Documentation",
            endpoints = endpoints
        });

        return app;
    }
}