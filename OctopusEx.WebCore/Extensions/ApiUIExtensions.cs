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
        String title = "API", 
        String version = "v1", 
        String description = "ASP.NET Core Web API",
        String xmlSearchPattern = "*.xml")
    {
        // 添加OpenAPI服务 (使用.NET 10的OpenAPI)
        services.AddOpenApi();
        
        services.AddSwaggerGen(c =>
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, xmlSearchPattern);
            foreach (var file in files)
            {
                c.IncludeXmlComments(file, true);
            }
            
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
    public static WebApplication UseSwaggerUI(this WebApplication app, 
        String swaggerVersion = "v1", 
        String routePrefix = "swagger")
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
    public static WebApplication UseScalarUI(this WebApplication app, 
        String title = "API Documentation",
        ScalarTheme theme = ScalarTheme.Default,
        ScalarLayout layout = ScalarLayout.Modern)
    {
        if (app.Environment.IsDevelopment())
        {
            // 添加 OpenAPI 端点
            app.MapOpenApi();
            
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
        String title = "API Documentation",
        String swaggerVersion = "v1",
        ScalarTheme scalarTheme = ScalarTheme.Default,
        ScalarLayout scalarLayout = ScalarLayout.Modern)
    {
        app.UseSwaggerUI(swaggerVersion, "swagger");
        app.UseScalarUI(title, scalarTheme, scalarLayout);
 

        return app;
    }

    /// <summary>
    /// 添加 API 文档导航页面
    /// </summary>
    public static WebApplication AddApiDocumentationNavigation(this WebApplication app, 
        Boolean enableSwagger = true, 
        Boolean enableScalar = true)
    {
        var endpoints = new Dictionary<String, String>();
        
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