namespace OctopusEx.WebCore.Interceptors;

using System.Collections.Concurrent;
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

    private void OnBeforeSaveChanges(DbContext context)
    {
        context.ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditEntry>();
        var now = DateTime.UtcNow;
        var userInfo = _configuration.GetCurrentUser();

        foreach ( var entry in context.ChangeTracker.Entries() )
        {
            // 跳过审计日志实体本身
            if ( entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged )
                continue;

            var entityType = entry.Entity.GetType();
            var domainName = GetDomainName(entityType);
            var domainConfig = _configuration.GetDomainConfiguration(domainName);

            // 检查该领域是否启用审计
            if ( !domainConfig.Enabled )
                continue;

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

            auditEntries.Add(auditEntry);

            foreach ( var property in entry.Properties )
            {
                if ( property.IsTemporary )
                    continue;

                string propertyName = property.Metadata.Name;

                // 检查是否应该忽略该字段
                if ( ShouldIgnoreProperty(propertyName, domainConfig) )
                    continue;

                // // 检查是否只跟踪特定字段（只有当TrackAllProperties为false时才检查）
                // if ( !domainConfig.TrackAllProperties && domainConfig.TrackedProperties.Any() && !domainConfig.TrackedProperties.Contains(propertyName) )
                //     continue;

                if ( property.Metadata.IsPrimaryKey() )
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue!;
                    continue;
                }

                switch ( entry.State )
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue!;
                        // if ( domainConfig.TrackAllProperties || domainConfig.TrackedProperties.Contains(propertyName) )
                        // {
                        //     auditEntry.ChangedProperties.Add(propertyName);
                        // }
                        // auditEntry.ChangedProperties.Add(propertyName);
                        auditEntry.Action = "INSERT";
                        break;

                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue!;
                        // if ( domainConfig.TrackAllProperties || domainConfig.TrackedProperties.Contains(propertyName) )
                        // {
                        //     auditEntry.ChangedProperties.Add(propertyName);
                        // }
                        auditEntry.ChangedProperties.Add(propertyName);
                        auditEntry.Action = "DELETE";
                        break;

                    case EntityState.Modified:
                        if ( property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue!;
                            auditEntry.NewValues[propertyName] = property.CurrentValue!;
                            // if ( domainConfig.TrackAllProperties || domainConfig.TrackedProperties.Contains(propertyName) )
                            // {
                            //     auditEntry.ChangedProperties.Add(propertyName);
                            // }
                            auditEntry.ChangedProperties.Add(propertyName);
                            auditEntry.Action = "UPDATE";
                        }
                        break;
                }
            }
        }

        // 将审计条目存储在静态存储中，以便在保存后使用
        AuditEntryStore.SetAuditEntries(context, auditEntries);
    }

    private void OnAfterSaveChanges(DbContext context)
    {
        var auditEntries = AuditEntryStore.GetAuditEntries(context);
        if ( auditEntries == null || auditEntries.Count == 0 )
            return;

        SaveAuditLogs(context, auditEntries).Wait();
        AuditEntryStore.ClearAuditEntries(context);
    }

    private async Task OnAfterSaveChangesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        var auditEntries = AuditEntryStore.GetAuditEntries(context);
        if ( auditEntries == null || auditEntries.Count == 0 )
            return;

        await SaveAuditLogs(context, auditEntries, cancellationToken);
        AuditEntryStore.ClearAuditEntries(context);
    }

    private async Task SaveAuditLogs(DbContext context, List<AuditEntry> auditEntries, CancellationToken cancellationToken = default)
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

        if ( auditLogs.Any() )
        {
            await context.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
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

public static class AuditEntryStore
{
    private static readonly ConcurrentDictionary<DbContext, List<AuditEntry>> _auditEntries = new();

    public static void SetAuditEntries(DbContext context, List<AuditEntry> auditEntries)
    {
        _auditEntries[context] = auditEntries;
    }

    public static List<AuditEntry> GetAuditEntries(DbContext context)
    {
        return _auditEntries.TryGetValue(context, out var entries) ? entries : new List<AuditEntry>();
    }

    public static void ClearAuditEntries(DbContext context)
    {
        _auditEntries.TryRemove(context, out _);
    }
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
