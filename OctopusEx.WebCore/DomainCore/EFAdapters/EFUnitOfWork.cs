namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using Repositories.Implementations.UnitOfWork;
using Repositories.Interfaces;

/// <summary>
/// EF Core工作单元实现
/// </summary>
public class EFUnitOfWork : UnitOfWorkBase
{
    public EFUnitOfWork(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 创建EF Core仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    protected override IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
    {
        return new EFRepository<TEntity, TKey>(DbContext);
    }

    // 可以在这里添加特定于EF Core的扩展方法
}
