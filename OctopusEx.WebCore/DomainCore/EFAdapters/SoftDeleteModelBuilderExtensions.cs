namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using SoftDelete;

/// <summary>
/// 软删除全局查询过滤器扩展。在 DbContext.OnModelCreating 中调用
/// modelBuilder.AddSoftDeleteFilter() 即可为所有实现 ISoftDelete 的实体
/// 自动注入 IsDeleted == false 的全局过滤条件。
/// 需要查询已删除数据时，调用 query.IgnoreQueryFilters()。
/// </summary>
public static class SoftDeleteModelBuilderExtensions
{
    public static ModelBuilder AddSoftDeleteFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            entityType.SetQueryFilter(lambda);
        }

        return modelBuilder;
    }
}
