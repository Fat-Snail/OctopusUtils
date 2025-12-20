using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        // Configure OpenAPI
        services.AddOpenApi(c =>
        {
            c.AddDocumentTransformer((document, context, _) =>
            {
                document.Info = new()
                {
                    Title = title,
                    Version = version,
                    Description = description,
                    Contact = new()
                    {
                        Name = "API Support",
                        Email = "api@example.com",
                        Url = new Uri("https://api.example.com/support")
                    }
                };
                return Task.CompletedTask;
            });
        });

        services.AddSwaggerGen(c =>
        {
            var files = Directory.GetFiles(AppContext.BaseDirectory, xmlSearchPattern);
            foreach (var file in files)
            {
                c.IncludeXmlComments(file, true);
            }

            c.AddAutoApiDescriptions();
            
            c.SwaggerDoc(version, new OpenApiInfo { Title = title, Version = version, Description = description });
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
                c.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json",
                    $"{app.Environment.ApplicationName} {swaggerVersion}");
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

        app.MapGet("/", () => new { message = "Welcome to API Documentation", endpoints = endpoints });

        return app;
    }
}

/// <summary>
/// 自动 API 描述扩展
/// </summary>
public static class ApiDescriptionExtensions
{
    /// <summary>
    /// 配置自动 API 描述
    /// </summary>
    public static void AddAutoApiDescriptions(this SwaggerGenOptions options)
    {
        options.OperationFilter<AutoEndpointDescriptionFilter>();
    }
}

/// <summary>
/// 自动端点描述过滤器
/// </summary>
public class AutoEndpointDescriptionFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.EndpointMetadata == null)
            return;

        // 如果已经有 EndpointSummary 或 EndpointDescription，跳过
        var hasExistingDescription = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .Any(em => em.GetType().Name.Contains("EndpointSummary") ||
                       em.GetType().Name.Contains("EndpointDescription"));

        if (hasExistingDescription)
            return;

        // 基于控制器名和方法名自动生成描述
        var controllerName = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        var actionName = context.ApiDescription.ActionDescriptor.RouteValues["action"];
        var method = context.ApiDescription.HttpMethod;

        // 生成默认摘要和描述
        var summary = GenerateSummary(controllerName, actionName, method);
        var description = GenerateDescription(controllerName, actionName, method);

        operation.Summary = summary;
        operation.Description = description;
    }

    private string GenerateSummary(string controller, string action, string method)
    {
        var controllerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Orders", "订单" },
            { "Products", "产品" },
            { "Users", "用户" },
            { "Data", "数据" },
            { "Jobs", "任务" }
        };

        var actionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Get", "获取" },
            { "Post", "创建" },
            { "Put", "更新" },
            { "Delete", "删除" },
            { "GetOrders", "获取订单列表" },
            { "GetProducts", "获取产品列表" },
            { "GetDashboardData", "获取仪表板数据" },
            { "GetCategoryStatistics", "获取分类销售统计" },
            { "GetCustomerSummary", "获取客户购买统计" },
            { "GetProductPerformance", "获取产品销售性能分析" },
            { "GetComplexQuery", "执行复杂查询" },
            { "GetSalesTrend", "获取销售趋势" }
        };

        if (actionMap.TryGetValue(action, out var actionDesc))
        {
            return actionDesc;
        }

        var controllerDesc = controllerMap.TryGetValue(controller, out var ctrlDesc) ? ctrlDesc : controller;
        return $"{controllerDesc}操作";
    }

    private string GenerateDescription(string controller, string action, string method)
    {
        var summary = GenerateSummary(controller, action, method);
        return $"{summary}的详细说明";
    }
}

/// <summary>
/// API 端点标记属性，用于简化重复的 EndpointSummary 和 EndpointDescription
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiEndpointAttribute : Attribute
{
    public string Summary { get; set; }
    public string Description { get; set; }

    public ApiEndpointAttribute(string summary, string description = null)
    {
        Summary = summary;
        Description = description ?? summary;
    }
}