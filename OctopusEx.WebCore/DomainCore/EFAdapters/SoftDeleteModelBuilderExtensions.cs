namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using SoftDelete;

/// <summary>
/// 软删除全局查询过滤器扩展。在 DbContext.OnModelCreating 中调用
/// modelBuilder.AddSoftDeleteFilter() 即可为所有实现 ISoftDelete 的实体
/// 自动注入 IsDeleted == false 的全局过滤条件。
///
/// 以命名过滤器注册（EF Core 10+ 的 SetQueryFilter(key, expression)），
/// 与多租户等其它过滤器由 EF Core 自动按 AND 组合，无需手工拼接表达式，
/// 重复调用也只会按 key 覆盖而不是叠加。
/// 需要查询已删除数据时，调用 query.IgnoreSoftDelete() 或 query.IgnoreQueryFilters()。
/// </summary>
public static class SoftDeleteModelBuilderExtensions
{
    /// <summary>软删除命名过滤器的 key。</summary>
    public const String SoftDeleteFilterKey = "Octopus:SoftDelete";

    public static ModelBuilder AddSoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var notDeleted = Expression.Equal(property, Expression.Constant(false));

            entityType.SetQueryFilter(SoftDeleteFilterKey, Expression.Lambda(notDeleted, parameter));
        }

        return modelBuilder;
    }
}

/// <summary>
/// 软删除查询扩展
/// </summary>
public static class SoftDeleteQueryableExtensions
{
    /// <summary>
    /// 忽略软删除过滤器，包含已删除数据。语义比 IgnoreQueryFilters() 更明确。
    /// 注意：这会同时忽略所有全局查询过滤器（如多租户），EF Core 不支持单独跳过其中一项。
    /// </summary>
    public static IQueryable<TEntity> IgnoreSoftDelete<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class, ISoftDelete
        => query.IgnoreQueryFilters();
}
