namespace OctopusEx.WebCore.MultiTenancy;

/// <summary>
/// 租户数据库连接字符串解析器。
/// 支持"每租户独立数据库"模式：根据 ICurrentTenant 返回对应连接串，DbContext 工厂调用本接口配置连接。
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>解析当前租户的连接字符串。租户上下文为空时返回默认连接串或抛异常（实现自行决定）。</summary>
    String Resolve();

    /// <summary>解析指定租户的连接字符串。</summary>
    String Resolve(String tenantId);
}

/// <summary>
/// 基于内存字典的连接路由实现。配置阶段注册 (tenantId, connectionString) 映射。
/// 适合租户数量少且固定的场景；动态租户应实现自定义 ITenantConnectionResolver。
/// </summary>
public class DictionaryTenantConnectionResolver : ITenantConnectionResolver
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IReadOnlyDictionary<String, String> _connectionStrings;
    private readonly String? _defaultConnectionString;

    public DictionaryTenantConnectionResolver(
        ICurrentTenant currentTenant,
        IReadOnlyDictionary<String, String> connectionStrings,
        String? defaultConnectionString = null)
    {
        _currentTenant = currentTenant;
        _connectionStrings = connectionStrings;
        _defaultConnectionString = defaultConnectionString;
    }

    public String Resolve()
    {
        var tenantId = _currentTenant.TenantId;
        if (String.IsNullOrEmpty(tenantId))
            return _defaultConnectionString
                ?? throw new InvalidOperationException("当前无租户上下文且未配置 default connection string");
        return Resolve(tenantId);
    }

    public String Resolve(String tenantId)
    {
        if (_connectionStrings.TryGetValue(tenantId, out var conn)) return conn;
        return _defaultConnectionString
            ?? throw new KeyNotFoundException($"未找到租户 {tenantId} 的连接字符串配置");
    }
}
