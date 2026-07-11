using OctopusEx.WebCore.MultiTenancy;

namespace OctopusEx.Sample.WebApi;

/// <summary>
/// 演示租户感知的 Hangfire 后台任务。
/// </summary>
public interface ITenantAwareHangfireJob
{
    Task RunAsync(string tenantId, Guid todoId, CancellationToken cancellationToken);
}

public class TenantAwareHangfireJob : ITenantAwareHangfireJob
{
    private readonly ILogger<TenantAwareHangfireJob> _logger;

    public TenantAwareHangfireJob(ILogger<TenantAwareHangfireJob> logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(string tenantId, Guid todoId, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Tenant-aware job running: TenantId={TenantId}, TodoId={TodoId}",
            tenantId, todoId);

        // 模拟异步处理
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation("Tenant-aware job completed: TodoId={TodoId}", todoId);
    }
}
