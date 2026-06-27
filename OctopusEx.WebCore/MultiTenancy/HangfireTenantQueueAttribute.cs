namespace OctopusEx.WebCore.MultiTenancy;

using Hangfire.Client;
using Hangfire.Common;
using Hangfire.States;

/// <summary>
/// Hangfire 任务多租户队列隔离过滤器。
///
/// 工作机制（IClientFilter + IElectStateFilter 组合）：
/// 1. 任务 enqueue 时（OnCreating），从 ICurrentTenant 读取当前租户 ID，写入 JobParameter "TenantId"
/// 2. 任务进入 Enqueued 状态时（OnStateElection），从 JobParameter 读出 TenantId，路由到 "tenant-{id}" 队列
///
/// 用 JobParameter 而非扫描 args，避免误把业务字符串当作租户 ID。
///
/// 用法：
/// <code>
/// // 注册到全局 filter，自动覆盖所有 enqueue 的任务
/// var accessor = sp.GetRequiredService&lt;ICurrentTenant&gt;();
/// GlobalJobFilters.Filters.Add(new HangfireTenantQueueAttribute(() =&gt; accessor.TenantId));
///
/// // 或仅标注在需要租户隔离的方法上
/// [HangfireTenantQueue]
/// public void SendInvoice(int orderId) { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class HangfireTenantQueueAttribute : JobFilterAttribute, IClientFilter, IElectStateFilter
{
    /// <summary>JobParameter 名称：客户端 enqueue 时写入，server 端 enqueue 状态读取</summary>
    public const String TenantIdParameterName = "TenantId";

    private const String QueuePrefix = "tenant-";
    private const String DefaultQueue = "default";

    private readonly Func<String?>? _tenantAccessor;

    /// <summary>Attribute 用法（无依赖注入）。需配合显式 SetJobParameter("TenantId", ...) 使用。</summary>
    public HangfireTenantQueueAttribute() { }

    /// <summary>Filter 用法（注册到 GlobalJobFilters，运行时通过 accessor 读取当前租户 ID）。</summary>
    public HangfireTenantQueueAttribute(Func<String?> tenantAccessor) => _tenantAccessor = tenantAccessor;

    /// <summary>客户端 enqueue 时写入 TenantId 参数</summary>
    public void OnCreating(CreatingContext context)
    {
        var tenantId = _tenantAccessor?.Invoke();
        if (!String.IsNullOrEmpty(tenantId))
            context.SetJobParameter(TenantIdParameterName, tenantId);
    }

    public void OnCreated(CreatedContext context) { }

    /// <summary>状态切换时读 TenantId 路由队列</summary>
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not EnqueuedState enqueued) return;

        var tenantId = context.GetJobParameter<String?>(TenantIdParameterName);
        enqueued.Queue = String.IsNullOrEmpty(tenantId) ? DefaultQueue : QueuePrefix + tenantId;
    }
}
