using OctopusEx.WebCore.DomainCore.SoftDelete;
using OctopusEx.WebCore.MultiTenancy;

namespace OctopusEx.Sample.WebApi.Models;

/// <summary>
/// 待办事项实体。演示 ISoftDelete / IMultiTenant / 审计日志。
/// </summary>
public class TodoItem : ISoftDelete, IMultiTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>租户 ID（多租户隔离）</summary>
    public string? TenantId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ISoftDelete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
