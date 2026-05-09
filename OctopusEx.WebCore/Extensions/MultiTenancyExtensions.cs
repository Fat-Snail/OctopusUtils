namespace OctopusEx.WebCore.Extensions;

using MultiTenancy;

/// <summary>多租户注册扩展</summary>
public static class MultiTenancyExtensions
{
    /// <summary>
    /// 注册多租户基础设施。默认组合：Header → JWT Claim → Subdomain（按顺序回退）。
    /// </summary>
    public static IServiceCollection AddSimpleMultiTenancy(
        this IServiceCollection services,
        Action<MultiTenancyOptions>? configure = null)
    {
        var options = new MultiTenancyOptions();
        configure?.Invoke(options);

        services.AddSingleton<ICurrentTenant, CurrentTenant>();

        if (options.Resolvers.Count == 0)
        {
            options.Resolvers.Add(new HeaderTenantResolver());
            options.Resolvers.Add(new JwtClaimTenantResolver());
            options.Resolvers.Add(new SubdomainTenantResolver());
        }
        services.AddSingleton<ITenantResolver>(new CompositeTenantResolver(options.Resolvers));

        return services;
    }

    /// <summary>注册多租户中间件。须在 UseAuthentication 之后、业务路由之前。</summary>
    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
        => app.UseMiddleware<MultiTenancyMiddleware>();
}

/// <summary>多租户配置</summary>
public class MultiTenancyOptions
{
    /// <summary>租户解析器组合（按顺序尝试，第一个非空胜出）。为空时使用默认 Header → JWT → Subdomain。</summary>
    public List<MultiTenancy.ITenantResolver> Resolvers { get; } = new();
}
