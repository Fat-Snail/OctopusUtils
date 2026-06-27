namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using SoftDelete;

/// <summary>
/// 软删除全局查询过滤器扩展。在 DbContext.OnModelCreating 中调用
/// modelBuilder.AddSoftDeleteFilter() 即可为所有实现 ISoftDelete 的实体
/// 自动注入 IsDeleted == false 的全局过滤条件。
///
/// 与已有过滤器（如多租户隔离）使用 AndAlso 合并，不会覆盖。
/// 需要查询已删除数据时，调用 query.IgnoreSoftDelete() 或 query.IgnoreQueryFilters()。
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
            var notDeleted = Expression.Equal(property, Expression.Constant(false));

            var existing = entityType.GetQueryFilter();
            LambdaExpression finalFilter;

            if (existing == null)
            {
                finalFilter = Expression.Lambda(notDeleted, parameter);
            }
            else
            {
                // 把已有过滤器的 body 中的参数替换为统一参数后用 AndAlso 合并
                var existingBody = ParameterReplacer.Replace(existing.Body, existing.Parameters[0], parameter);
                var combined = Expression.AndAlso(existingBody, notDeleted);
                finalFilter = Expression.Lambda(combined, parameter);
            }

            entityType.SetQueryFilter(finalFilter);
        }

        return modelBuilder;
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        private ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        public static Expression Replace(Expression body, ParameterExpression from, ParameterExpression to)
            => new ParameterReplacer(from, to).Visit(body);

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
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
