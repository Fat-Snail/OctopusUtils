namespace OctopusEx.WebCore.MultiTenancy;

/// <summary>
/// 租户解析策略。可组合多个策略：HeaderTenantResolver → JwtClaimTenantResolver → SubdomainTenantResolver
/// </summary>
public interface ITenantResolver
{
    /// <summary>从当前 HttpContext 解析租户 ID；无法识别返回 null。</summary>
    String? Resolve(HttpContext context);
}

/// <summary>从 HTTP Header 解析（默认 X-Tenant-Id）</summary>
public class HeaderTenantResolver : ITenantResolver
{
    private readonly String _headerName;
    public HeaderTenantResolver(String headerName = "X-Tenant-Id") => _headerName = headerName;
    public String? Resolve(HttpContext context)
        => context.Request.Headers.TryGetValue(_headerName, out var value) ? value.ToString() : null;
}

/// <summary>从 Query string 解析（默认 ?tenant=xxx）</summary>
public class QueryTenantResolver : ITenantResolver
{
    private readonly String _key;
    public QueryTenantResolver(String key = "tenant") => _key = key;
    public String? Resolve(HttpContext context)
        => context.Request.Query.TryGetValue(_key, out var value) ? value.ToString() : null;
}

/// <summary>从子域名解析（如 acme.example.com → acme）</summary>
public class SubdomainTenantResolver : ITenantResolver
{
    public String? Resolve(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : null;
    }
}

/// <summary>从 JWT Claim 解析（默认 tenant_id claim）</summary>
public class JwtClaimTenantResolver : ITenantResolver
{
    private readonly String _claimType;
    public JwtClaimTenantResolver(String claimType = "tenant_id") => _claimType = claimType;
    public String? Resolve(HttpContext context)
        => context.User.FindFirst(_claimType)?.Value;
}

/// <summary>组合多个 resolver，按顺序尝试，返回第一个非空结果</summary>
public class CompositeTenantResolver : ITenantResolver
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;
    public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
        => _resolvers = resolvers.ToList();

    public String? Resolve(HttpContext context)
    {
        foreach (var r in _resolvers)
        {
            var id = r.Resolve(context);
            if (!String.IsNullOrEmpty(id)) return id;
        }
        return null;
    }
}
