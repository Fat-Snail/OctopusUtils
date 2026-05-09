namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using Repositories.Implementations.Repositories;
using Repositories.Interfaces;
using SoftDelete;

/// <summary>
/// EF Core仓储实现，支持软删除
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class EFRepository<TEntity, TKey> : RepositoryBase<TEntity, TKey> where TEntity : class where TKey : notnull
{
    public EFRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// 删除实体。若实体实现 ISoftDelete，则软删除（设置 IsDeleted = true）；否则物理删除。
    /// </summary>
    public override async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletedAt = DateTime.UtcNow;
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }
        else
        {
            await base.DeleteAsync(entity, cancellationToken);
        }
    }
}
