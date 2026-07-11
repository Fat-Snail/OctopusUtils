namespace OctopusEx.WebCore.Extensions.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using MultiTenancy;

/// <summary>
/// 多租户健康检查：验证 ICurrentTenant 与 ITenantConnectionResolver 是否正确注册。
/// </summary>
public class TenantHealthCheck : ICustomHealthCheck
{
    private readonly ICurrentTenant? _currentTenant;
    private readonly ITenantConnectionResolver? _connectionResolver;

    public TenantHealthCheck(ICurrentTenant? currentTenant = null, ITenantConnectionResolver? connectionResolver = null)
    {
        _currentTenant = currentTenant;
        _connectionResolver = connectionResolver;
    }

    public String Name => "tenant";
    public String[] Tags => ["tenant", "ready"];
    public Int32 TimeoutSeconds => 5;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<String, Object>();

        try
        {
            // 检查 ICurrentTenant 是否已注册
            if (_currentTenant == null)
            {
                data["currentTenant"] = "not registered";
                return HealthCheckResult.Degraded("ICurrentTenant is not registered (multi-tenancy not enabled or misconfigured)", null, data);
            }

            var tenantId = _currentTenant.TenantId;
            data["currentTenantId"] = tenantId ?? "(none)";

            // 检查 ITenantConnectionResolver（如果注册）
            if (_connectionResolver == null)
            {
                data["connectionResolver"] = "not registered";
                // 未注册并非错误：单数据库共享模式不需要 resolver
                return HealthCheckResult.Healthy("Multi-tenancy infrastructure registered (shared database mode)", data);
            }

            data["connectionResolver"] = _connectionResolver.GetType().Name;

            // 验证能否解析连接字符串（仅当有当前租户时）
            if (!String.IsNullOrEmpty(tenantId))
            {
                try
                {
                    _connectionResolver.Resolve(tenantId);
                    data["tenantConnectionResolved"] = true;
                }
                catch (KeyNotFoundException)
                {
                    data["tenantConnectionResolved"] = false;
                    data["error"] = $"Connection not configured for tenant '{tenantId}'";
                    return HealthCheckResult.Degraded($"Tenant '{tenantId}' has no connection configured", null, data);
                }
            }

            return HealthCheckResult.Healthy("Multi-tenancy infrastructure healthy", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Multi-tenancy health check failed", ex, data);
        }
    }
}
