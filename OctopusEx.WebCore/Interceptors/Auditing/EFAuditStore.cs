namespace OctopusEx.WebCore.Interceptors.Auditing;

/// <summary>
/// EF Core 审计日志存储实现。通过 DbContext 将审计日志写入 <c>audit_logs</c> 表。
/// 支持按租户 / 时间分区，并与业务事务同事务落库。
/// </summary>
public class EFAuditStore : IAuditStore
{
    private readonly DbContext _dbContext;

    public EFAuditStore(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(IEnumerable<AuditLog> logs, CancellationToken cancellationToken = default)
    {
        var list = logs.ToList();
        if (list.Count == 0) return;
        await _dbContext.Set<AuditLog>().AddRangeAsync(list, cancellationToken);
    }

    public async Task<Int32> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var cutoffUtc = cutoff.UtcDateTime;
        var logs = _dbContext.Set<AuditLog>().Where(e => e.Timestamp < cutoffUtc);
        _dbContext.Set<AuditLog>().RemoveRange(logs);
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Int64> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<AuditLog>().LongCountAsync(cancellationToken);
    }
}

/// <summary>
/// EF Core 模型配置扩展。在 DbContext.OnModelCreating 中调用
/// <c>modelBuilder.AddOctopusAudit()</c> 即可自动注册审计日志表。
/// </summary>
public static class AuditModelBuilderExtensions
{
    /// <summary>
    /// 为当前 DbContext 注册 audit_logs 审计日志表。
    /// 自动配置：
    /// - 主键 Id (long, identity)
    /// - 复合索引 IX_AuditLogs_Timestamp（用于范围查询和清理）
    /// - 索引 IX_AuditLogs_Tenant（按租户 / 表名过滤）
    /// </summary>
    public static ModelBuilder AddOctopusAudit(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedOnAdd();
            b.Property(e => e.TableName).HasMaxLength(200).IsRequired();
            b.Property(e => e.DomainName).HasMaxLength(200);
            b.Property(e => e.EntityId).HasMaxLength(100).IsRequired();
            b.Property(e => e.Action).HasMaxLength(20).IsRequired();
            b.Property(e => e.UserId).HasMaxLength(100);
            b.Property(e => e.UserName).HasMaxLength(200);
            b.Property(e => e.IpAddress).HasMaxLength(100);
            b.Property(e => e.UserAgent).HasMaxLength(1000);
            b.Property(e => e.Changes).HasColumnType("text");
            b.Property(e => e.OldValues).HasColumnType("text");
            b.Property(e => e.NewValues).HasColumnType("text");

            // 时间索引（范围查询 + 清理）
            b.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");

            // 租户 / 表名索引
            b.HasIndex(e => new { e.DomainName, e.TableName, e.Timestamp })
                .HasDatabaseName("IX_AuditLogs_Tenant_Time");
        });

        return modelBuilder;
    }
}
