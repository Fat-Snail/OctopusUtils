namespace OctopusEx.WebCore.Interceptors.Auditing;

/// <summary>
/// 审计日志持久化存储抽象。生产实现将审计条目写入独立数据库表。
/// </summary>
public interface IAuditStore
{
    /// <summary>批量写入审计日志（通常在 SaveChanges 事务内调用）。</summary>
    Task WriteAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default);

    /// <summary>删除指定时间之前的审计日志，返回删除数量。</summary>
    Task<Int32> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    /// <summary>审计日志总数。</summary>
    Task<Int64> CountAsync(CancellationToken cancellationToken = default);
}

/// <summary>审计日志保留策略配置</summary>
public class AuditRetentionOptions
{
    /// <summary>保留天数。默认 90 天。</summary>
    public Int32 RetentionDays { get; set; } = 90;

    /// <summary>每日清理的执行时间（UTC）。默认凌晨 3:00。</summary>
    public TimeSpan CleanupTimeUtc { get; set; } = TimeSpan.FromHours(3);

    /// <summary>是否启用自动清理。</summary>
    public Boolean EnableAutoCleanup { get; set; } = true;
}
