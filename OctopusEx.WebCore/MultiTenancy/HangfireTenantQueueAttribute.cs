namespace OctopusEx.WebCore.MultiTenancy;

using Hangfire.Common;
using Hangfire.States;

/// <summary>
/// 多租户 Hangfire 任务过滤器：把 enqueue 的任务路由到 "tenant-{currentTenantId}" 队列，
/// 实现租户级任务隔离（不同租户的任务互不干扰，可分配独立 worker）。
///
/// 用法：
/// <code>
/// JobStorage.Current = ...;
/// GlobalJobFilters.Filters.Add(new HangfireTenantQueueAttribute(currentTenantAccessor));
/// </code>
/// 或者标注在具体方法上：
/// <code>
/// [HangfireTenantQueue]
/// public void SendInvoice(int orderId) { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class HangfireTenantQueueAttribute : JobFilterAttribute, IElectStateFilter
{
    private const String QueuePrefix = "tenant-";
    private const String DefaultQueue = "default";

    private readonly Func<String?>? _tenantAccessor;

    /// <summary>构造函数。Attribute 用法（无依赖注入）：通过 JobStorage.Current 等其他方式获取租户。</summary>
    public HangfireTenantQueueAttribute() { }

    /// <summary>构造函数。Filter 用法（注册到 GlobalJobFilters，传入访问器读取当前租户）。</summary>
    public HangfireTenantQueueAttribute(Func<String?> tenantAccessor) => _tenantAccessor = tenantAccessor;

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not EnqueuedState enqueued) return;

        var tenantId = _tenantAccessor?.Invoke();
        // 优先用 attribute 上传入的访问器；其次从 Job 参数读取（约定：第一个 string 参数若以 tenant- 开头）
        if (String.IsNullOrEmpty(tenantId))
        {
            tenantId = ExtractFromJobParameters(context);
        }

        enqueued.Queue = String.IsNullOrEmpty(tenantId) ? DefaultQueue : QueuePrefix + tenantId;
    }

    private static String? ExtractFromJobParameters(ElectStateContext context)
    {
        var tenantParam = context.BackgroundJob?.Job?.Args?.OfType<String>()
            .FirstOrDefault(s => s != null && s.StartsWith(QueuePrefix, StringComparison.Ordinal));
        return tenantParam?.Substring(QueuePrefix.Length);
    }
}
