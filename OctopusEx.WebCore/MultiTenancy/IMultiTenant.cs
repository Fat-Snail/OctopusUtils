namespace OctopusEx.WebCore.MultiTenancy;

/// <summary>
/// 多租户实体标记接口。实体实现此接口后，EF Core 全局过滤器自动注入
/// "TenantId == 当前租户" 过滤条件，无需业务代码显式判断。
/// </summary>
public interface IMultiTenant
{
    /// <summary>租户标识。新实体保存时由 SaveChangesInterceptor 自动填充。</summary>
    String TenantId { get; set; }
}
