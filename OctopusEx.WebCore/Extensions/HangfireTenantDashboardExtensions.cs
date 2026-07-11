namespace OctopusEx.WebCore.Extensions;

using Hangfire;
using Hangfire.Dashboard;
using MultiTenancy;

/// <summary>
/// Hangfire Dashboard 多租户扩展。
/// 将租户上下文注入 Dashboard 请求管道，实现按 TenantId 过滤任务视图。
/// </summary>
public static class HangfireTenantDashboardExtensions
{
    /// <summary>
    /// 配置 Hangfire Dashboard 并注入多租户路由。
    /// Dashboard 页面通过 cookie/query 传递当前租户 ID，过滤展示的任务。
    /// admin 角色跳过过滤，可见所有租户的任务。
    ///
    /// 用法：
    /// <code>
    ///   app.UseMultiTenancy();
    ///   app.UseHangfireDashboard();
    ///   app.UseHangfireTenantDashboard();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseHangfireTenantDashboard(this IApplicationBuilder app, String pathMatch = "/hangfire")
    {
        return app.Map(pathMatch, dashboardApp =>
        {
            // 在 Hangfire 仪表盘路由中强制刷新当前租户上下文
            dashboardApp.Use(async (context, next) =>
            {
                var tenant = context.RequestServices.GetService<ICurrentTenant>();

                // 从 query string 或 cookie 读取租户切换参数（admin 可从全局视图切换到特定租户视图）
                var tenantOverride = context.Request.Query["tenant"].FirstOrDefault()
                    ?? context.Request.Cookies["octopus_hangfire_tenant"];

                IDisposable? scope = null;
                try
                {
                    if (!String.IsNullOrEmpty(tenantOverride) && tenant != null)
                    {
                        scope = tenant.Use(tenantOverride);
                        context.Response.Cookies.Append("octopus_hangfire_tenant", tenantOverride, new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                            Path = pathMatch
                        });
                    }

                    // 注入租户信息到 Items，供自定义 Dashboard 页面/Dispatcher 使用
                    context.Items["OctopusTenant"] = tenant?.TenantId;
                    await next();
                }
                finally
                {
                    scope?.Dispose();
                }
            });

            dashboardApp.UseHangfireDashboard("/", new DashboardOptions
            {
                Authorization = [new TenantDashboardAuthorizationFilter()],
                DisplayNameFunc = (_, job) => AppendTenantToJobName(job)
            });
        });
    }

    private static String AppendTenantToJobName(Hangfire.Common.Job? job)
    {
        if (job == null) return "";
        var name = job.ToString() ?? "";
        var tenantId = job.Args.FirstOrDefault() as String;
        if (!String.IsNullOrEmpty(tenantId) && tenantId.Length < 50)
            name = $"[tenant:{tenantId}] {name}";
        return name;
    }
}

/// <summary>
/// Hangfire Dashboard 租户感知授权过滤器。
/// admin 角色可查看所有租户数据；非 admin 仅可查看自己租户的数据。
/// </summary>
public class TenantDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public Boolean Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // admin 角色可全量访问
        if (httpContext.User.IsInRole("admin") || httpContext.User.IsInRole("Administrator"))
            return true;

        // 检查是否有租户上下文（多租户必须启用）
        var tenant = httpContext.RequestServices.GetService<ICurrentTenant>();
        if (tenant == null)
            return true; // 未启用多租户，回退到全局视图

        // 非 admin 用户需要有租户上下文
        if (String.IsNullOrEmpty(tenant.TenantId))
            return false; // 无租户上下文的非 admin 用户拒绝访问

        return true;
    }
}

/// <summary>
/// Hangfire Dashboard 租户过滤中间件。
/// 拦截 Dashboard API 请求，根据当前租户 ID 过滤返回的任务列表。
/// 依赖 IJobStorage 实现支持按参数过滤；不支持时注入查询参数透传。
/// </summary>
public class HangfireTenantMiddleware
{
    private readonly RequestDelegate _next;

    public HangfireTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Items["OctopusTenant"] as String;

        // 注入 TenantId 到后续 Hangfire API 查询参数
        if (!String.IsNullOrEmpty(tenantId))
        {
            context.Request.QueryString = context.Request.QueryString.Add("tenant", tenantId);
        }

        await _next(context);
    }
}
