namespace OctopusEx.WebCore.Extensions;

using Interceptors;
using Interceptors.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 审计服务扩展方法
/// </summary>
public static class AuditServiceExtensions
{
    /// <summary>
    /// 添加审计服务
    /// </summary>
    public static IServiceCollection AddAuditing(this IServiceCollection services,
        Action<DefaultAuditConfiguration>? configure = null)
    {
        // 注册审计配置（通过工厂模式，依赖注入 IHttpContextAccessor 获取当前请求上下文）
        services.AddSingleton<IAuditConfiguration>(sp =>
        {
            var httpAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var cfg = new DefaultAuditConfiguration(httpAccessor);
            configure?.Invoke(cfg);
            return cfg;
        });

        // 注册审计拦截器
        services.AddScoped<AuditInterceptor>();

        return services;
    }

    /// <summary>
    /// 配置DbContext使用审计拦截器
    /// </summary>
    public static DbContextOptionsBuilder UseAuditing(this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        var auditInterceptor = serviceProvider.GetService<AuditInterceptor>();
        if ( auditInterceptor != null )
        {
            optionsBuilder.AddInterceptors(auditInterceptor);
        }

        return optionsBuilder;
    }
}
