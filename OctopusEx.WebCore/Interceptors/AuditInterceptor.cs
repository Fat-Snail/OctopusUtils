namespace OctopusEx.WebCore.Interceptors;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IAuditConfiguration _configuration;

    public AuditInterceptor(IAuditConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if ( _configuration.Enabled && eventData.Context != null )
        {
            OnBeforeSaveChanges(eventData.Context);
        }

        return result;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if ( _configuration.Enabled && eventData.Context != null )
        {
            OnBeforeSaveChanges(eventData.Context);
        }

        return result;
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if ( _configuration.Enabled && eventData.Context != null )
        {
            OnAfterSaveChanges(eventData.Context);
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if ( _configuration.Enabled && eventData.Context != null )
        {
            await OnAfterSaveChangesAsync(eventData.Context, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// 保存变更前：检测变更并收集所有需要审计的实体条目。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    private void OnBeforeSaveChanges(DbContext context)
    {
        context.ChangeTracker.DetectChanges();

        var auditEntries = CreateAuditEntries(context);
        AuditEntryStore.SetAuditEntries(context, auditEntries);
    }

    /// <summary>
    /// 遍历所有实体条目，为需要审计的实体创建 <see cref="AuditEntry"/>。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    /// <returns>本次保存需要记录的审计条目列表。</returns>
    private List<AuditEntry> CreateAuditEntries(DbContext context)
    {
        var now = DateTime.UtcNow;
        var userInfo = _configuration.GetCurrentUser();
        var auditEntries = new List<AuditEntry>();

        foreach ( var entry in context.ChangeTracker.Entries() )
        {
            var auditEntry = CreateAuditEntryForEntity(entry, now, userInfo);
            if ( auditEntry != null )
            {
                auditEntries.Add(auditEntry);
            }
        }

        return auditEntries;
    }

    /// <summary>
    /// 为单个实体条目创建审计条目。
    /// </summary>
    /// <param name="entry">实体条目。</param>
    /// <param name="now">当前时间戳（UTC）。</param>
    /// <param name="userInfo">当前用户信息。</param>
    /// <returns>审计条目；若该实体不需要审计则返回 <c>null</c>。</returns>
    private AuditEntry? CreateAuditEntryForEntity(EntityEntry entry, DateTime now, AuditUserInfo userInfo)
    {
        // 跳过审计日志实体本身以及未参与变更跟踪的实体
        if ( entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged )
            return null;

        var entityType = entry.Entity.GetType();
        var domainName = GetDomainName(entityType);
        var domainConfig = _configuration.GetDomainConfiguration(domainName);

        // 检查该领域是否启用审计
        if ( !domainConfig.Enabled )
            return null;

        var auditEntry = new AuditEntry(entry)
        {
            TableName = entityType.Name,
            DomainName = domainName,
            UserId = userInfo.UserId,
            UserName = userInfo.UserName,
            IpAddress = userInfo.IpAddress,
            UserAgent = userInfo.UserAgent,
            Timestamp = now
        };

        foreach ( var property in entry.Properties )
        {
            ProcessProperty(auditEntry, property, domainConfig);
        }

        return auditEntry;
    }

    /// <summary>
    /// 处理单个属性的审计逻辑（主键记录、Added/Deleted/Modified 分支）。
    /// </summary>
    /// <param name="auditEntry">所属的审计条目。</param>
    /// <param name="property">实体属性条目。</param>
    /// <param name="domainConfig">领域审计配置。</param>
    private void ProcessProperty(AuditEntry auditEntry, PropertyEntry property, DomainAuditConfiguration domainConfig)
    {
        if ( property.IsTemporary )
            return;

        string propertyName = property.Metadata.Name;

        // 检查是否应该忽略该字段
        if ( ShouldIgnoreProperty(propertyName, domainConfig) )
            return;

        if ( property.Metadata.IsPrimaryKey() )
        {
            auditEntry.KeyValues[propertyName] = property.CurrentValue!;
            return;
        }

        // 实体状态由所属 EntityEntry 决定，同一实体的所有属性共享同一状态
        switch ( property.EntityEntry.State )
        {
            case EntityState.Added:
                auditEntry.NewValues[propertyName] = property.CurrentValue!;
                auditEntry.Action = "INSERT";
                break;

            case EntityState.Deleted:
                auditEntry.OldValues[propertyName] = property.OriginalValue!;
                auditEntry.ChangedProperties.Add(propertyName);
                auditEntry.Action = "DELETE";
                break;

            case EntityState.Modified:
                if ( property.IsModified )
                {
                    auditEntry.OldValues[propertyName] = property.OriginalValue!;
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                    auditEntry.ChangedProperties.Add(propertyName);
                    auditEntry.Action = "UPDATE";
                }
                break;
        }
    }

    /// <summary>
    /// 保存变更后（同步）：将审计条目持久化为审计日志。
    /// 不再通过 <c>.Wait()</c> 阻塞异步方法，避免 sync-over-async 死锁风险。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    private void OnAfterSaveChanges(DbContext context)
    {
        var auditEntries = AuditEntryStore.GetAuditEntries(context);
        if ( auditEntries == null || auditEntries.Count == 0 )
            return;

        var auditLogs = BuildAuditLogs(auditEntries);
        if ( auditLogs.Any() )
        {
            context.Set<AuditLog>().AddRange(auditLogs);
            context.SaveChanges();
        }

        AuditEntryStore.ClearAuditEntries(context);
    }

    /// <summary>
    /// 保存变更后（异步）：将审计条目持久化为审计日志。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task OnAfterSaveChangesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var auditEntries = AuditEntryStore.GetAuditEntries(context);
        if ( auditEntries == null || auditEntries.Count == 0 )
            return;

        await SaveAuditLogs(context, auditEntries, cancellationToken);
        AuditEntryStore.ClearAuditEntries(context);
    }

    /// <summary>
    /// 异步持久化审计日志。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    /// <param name="auditEntries">待持久化的审计条目。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task SaveAuditLogs(DbContext context, List<AuditEntry> auditEntries, CancellationToken cancellationToken = default)
    {
        var auditLogs = BuildAuditLogs(auditEntries);
        if ( auditLogs.Any() )
        {
            await context.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 将审计条目转换为可持久化的 <see cref="AuditLog"/> 列表。
    /// 同步与异步保存路径共享此逻辑，确保审计日志格式一致。
    /// </summary>
    /// <param name="auditEntries">审计条目列表。</param>
    /// <returns>审计日志列表。</returns>
    private List<AuditLog> BuildAuditLogs(List<AuditEntry> auditEntries)
    {
        var auditLogs = new List<AuditLog>();

        foreach ( var auditEntry in auditEntries )
        {
            var changes = auditEntry.ChangedProperties.Select(p => new PropertyChange
            {
                PropertyName = p,
                OldValue = auditEntry.OldValues.ContainsKey(p) ? auditEntry.OldValues[p]?.ToString() : null,
                NewValue = auditEntry.NewValues.ContainsKey(p) ? auditEntry.NewValues[p]?.ToString() : null
            }).ToList();

            var auditLog = new AuditLog
            {
                TableName = auditEntry.TableName,
                DomainName = auditEntry.DomainName,
                EntityId = auditEntry.KeyValues.Values.FirstOrDefault()?.ToString() ?? "",
                Action = auditEntry.Action,
                Timestamp = auditEntry.Timestamp,
                UserId = auditEntry.UserId,
                UserName = auditEntry.UserName,
                IpAddress = auditEntry.IpAddress,
                UserAgent = auditEntry.UserAgent,
                Changes = JsonSerializer.Serialize(changes),
                OldValues = auditEntry.OldValues.Any() ? JsonSerializer.Serialize(auditEntry.OldValues) : null,
                NewValues = auditEntry.NewValues.Any() ? JsonSerializer.Serialize(auditEntry.NewValues) : null
            };

            auditLogs.Add(auditLog);
        }

        return auditLogs;
    }

    /// <summary>
    /// 根据实体类型获取领域名称
    /// </summary>
    private string GetDomainName(Type entityType)
    {
        // 使用实体类型名称作为领域名称，确保每个实体类型都有独立的配置
        return entityType.Name;
    }

    /// <summary>
    /// 检查是否应该忽略该字段
    /// </summary>
    private bool ShouldIgnoreProperty(string propertyName, DomainAuditConfiguration domainConfig)
    {
        // 全局忽略字段
        if ( _configuration.GlobalIgnoredProperties.Contains(propertyName) )
            return true;

        // 领域特定忽略字段
        if ( domainConfig.IgnoredProperties.Contains(propertyName) )
            return true;

        return false;
    }
}

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string TableName { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public Dictionary<string, object> KeyValues { get; } = new();
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<string> ChangedProperties { get; } = new();
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// 以 <see cref="DbContext"/> 为键的审计条目临时存储。
/// 使用 <see cref="ConditionalWeakTable{TKey, TValue}"/> 持有对 DbContext 的弱引用，
/// 当 DbContext 被垃圾回收时（例如 SaveChanges 抛出异常导致清理未执行），
/// 对应的审计条目会随之自动回收，避免静态字典造成的内存泄漏。
/// </summary>
public static class AuditEntryStore
{
    private static readonly ConditionalWeakTable<DbContext, List<AuditEntry>> _auditEntries = new();

    /// <summary>
    /// 设置（替换）指定上下文对应的审计条目。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    /// <param name="auditEntries">审计条目列表。</param>
    public static void SetAuditEntries(DbContext context, List<AuditEntry> auditEntries)
    {
        _auditEntries.Remove(context);
        _auditEntries.TryAdd(context, auditEntries);
    }

    /// <summary>
    /// 获取指定上下文对应的审计条目；若不存在则返回空列表。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    /// <returns>审计条目列表。</returns>
    public static List<AuditEntry> GetAuditEntries(DbContext context)
    {
        return _auditEntries.TryGetValue(context, out var entries) ? entries : new List<AuditEntry>();
    }

    /// <summary>
    /// 清除指定上下文对应的审计条目。
    /// </summary>
    /// <param name="context">数据库上下文。</param>
    public static void ClearAuditEntries(DbContext context)
    {
        _auditEntries.Remove(context);
    }
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
