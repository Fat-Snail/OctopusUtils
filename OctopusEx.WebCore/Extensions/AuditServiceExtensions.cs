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
        // 注册审计配置
        var config = new DefaultAuditConfiguration();
        configure?.Invoke(config);
        services.AddSingleton<IAuditConfiguration>(config);

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
