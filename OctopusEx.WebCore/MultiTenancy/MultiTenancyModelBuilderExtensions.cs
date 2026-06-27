namespace OctopusEx.WebCore.MultiTenancy;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// 多租户全局查询过滤器扩展。
///
/// ⚠️ 重要：EF Core 仅会在闭包 root 在 DbContext 实例上时按查询重新求值过滤器。
/// 因此最稳定的做法是把 ICurrentTenant 作为 DbContext 字段，直接在 OnModelCreating
/// 写闭包：
/// <code>
/// public class MyContext : DbContext {
///     private readonly ICurrentTenant _tenant;
///     protected override void OnModelCreating(ModelBuilder mb) {
///         mb.Entity&lt;Post&gt;().HasQueryFilter(
///             p =&gt; p.TenantId == _tenant.TenantId || _tenant.TenantId == null);
///     }
/// }
/// </code>
///
/// 本扩展提供的 HasMultiTenantFilter / AddMultiTenantFilter 是便捷封装，
/// 适用于 ICurrentTenant 注册为 Singleton 的常见场景；多 DbContext 实例并发
/// 切换租户的极端用例下推荐使用上面的直接写法。
///
/// 与软删除等已有过滤器使用 AndAlso 合并，不会互相覆盖。
/// </summary>
public static class MultiTenancyModelBuilderExtensions
{
    /// <summary>
    /// 在指定实体上注册多租户过滤器。
    /// C# 编译器从 lambda 生成的闭包结构能被 EF Core ParameterExtractingExpressionVisitor
    /// 正确识别为可参数化过滤器，每次查询重新读取 tenant.TenantId。
    /// </summary>
    public static ModelBuilder HasMultiTenantFilter<TEntity>(
        this ModelBuilder modelBuilder,
        ICurrentTenant tenant)
        where TEntity : class, IMultiTenant
    {
        // 包装成委托：EF Core ParameterExtractingExpressionVisitor 将 method-call 视为
        // 每查询重新求值的参数，避免把 tenant.TenantId 作为闭包字段被快照成常量。
        Func<String?> getTenantId = () => tenant.TenantId;

        var builder = modelBuilder.Entity<TEntity>();
        var existing = builder.Metadata.GetQueryFilter();

        if (existing == null)
        {
            builder.HasQueryFilter(e => e.TenantId == getTenantId() || getTenantId() == null);
        }
        else
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var existingBody = ParameterReplacer.Replace(existing.Body, existing.Parameters[0], parameter);
            Expression<Func<TEntity, Boolean>> tenantFilter =
                e => e.TenantId == getTenantId() || getTenantId() == null;
            var tenantBody = ParameterReplacer.Replace(tenantFilter.Body, tenantFilter.Parameters[0], parameter);
            var combined = Expression.Lambda<Func<TEntity, Boolean>>(
                Expression.AndAlso(existingBody, tenantBody), parameter);
            builder.HasQueryFilter(combined);
        }

        return modelBuilder;
    }

    /// <summary>
    /// 自动扫描 model 中所有实现 IMultiTenant 的实体并注册过滤器。
    /// 内部按实体类型分发到 <see cref="HasMultiTenantFilter{TEntity}"/>。
    /// </summary>
    public static ModelBuilder AddMultiTenantFilter(this ModelBuilder modelBuilder, ICurrentTenant currentTenant)
    {
        var applyMethod = typeof(MultiTenancyModelBuilderExtensions)
            .GetMethod(nameof(HasMultiTenantFilter), BindingFlags.Public | BindingFlags.Static)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType)) continue;

            applyMethod.MakeGenericMethod(entityType.ClrType)
                .Invoke(null, new Object[] { modelBuilder, currentTenant });
        }

        return modelBuilder;
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from, _to;
        private ParameterReplacer(ParameterExpression from, ParameterExpression to) { _from = from; _to = to; }

        public static Expression Replace(Expression body, ParameterExpression from, ParameterExpression to)
            => new ParameterReplacer(from, to).Visit(body);

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _from ? _to : base.VisitParameter(node);
    }
}
