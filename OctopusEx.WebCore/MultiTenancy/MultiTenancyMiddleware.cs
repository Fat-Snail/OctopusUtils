namespace OctopusEx.WebCore.MultiTenancy;

/// <summary>
/// 请求开始时调用 ITenantResolver 解析租户，写入 ICurrentTenant 上下文，
/// 后续业务代码（仓储、服务）从 ICurrentTenant 读取即可。
/// 注册：app.UseMultiTenancy() 应放在 UseAuthentication 之后（若用 JwtClaimTenantResolver）。
/// </summary>
public class MultiTenancyMiddleware
{
    private readonly RequestDelegate _next;

    public MultiTenancyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver, ICurrentTenant currentTenant)
    {
        var tenantId = resolver.Resolve(context);
        if (currentTenant is CurrentTenant impl)
            impl.TenantId = tenantId;
        await _next(context);
    }
}
