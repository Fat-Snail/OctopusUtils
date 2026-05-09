namespace OctopusEx.WebCore.DomainCore.SoftDelete;

/// <summary>
/// 软删除标记接口。实现此接口的实体在调用 DeleteAsync 时不会物理删除，
/// 而是设置 IsDeleted = true 并记录删除时间。
/// EF Core 全局查询过滤器会自动过滤已删除数据，查询时默认不返回已删除记录。
/// </summary>
public interface ISoftDelete
{
    /// <summary>是否已删除</summary>
    Boolean IsDeleted { get; set; }

    /// <summary>删除时间（UTC）</summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>删除操作人</summary>
    String? DeletedBy { get; set; }
}
