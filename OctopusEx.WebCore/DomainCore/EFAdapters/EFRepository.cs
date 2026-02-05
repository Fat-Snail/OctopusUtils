namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using Repositories.Implementations.Repositories;
using Repositories.Interfaces;

/// <summary>
/// EF Core仓储实现
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class EFRepository<TEntity, TKey> : RepositoryBase<TEntity, TKey> where TEntity : class where TKey : notnull
{
    public EFRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    // 可以在这里添加特定于EF Core的扩展方法
}
