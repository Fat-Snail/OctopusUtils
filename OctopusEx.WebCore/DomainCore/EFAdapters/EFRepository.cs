namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using Helpers;
using Repositories.Implementations.Repositories;
using Repositories.Interfaces;
using SoftDelete;

/// <summary>
/// EF Core仓储实现，支持软删除（自动填充 DeletedBy 来自 ICurrentUser）
/// </summary>
public class EFRepository<TEntity, TKey> : RepositoryBase<TEntity, TKey> where TEntity : class where TKey : notnull
{
    private readonly ICurrentUser? _currentUser;

    public EFRepository(IDbContext dbContext) : base(dbContext)
    {
    }

    public EFRepository(IDbContext dbContext, ICurrentUser currentUser) : base(dbContext)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// 删除实体。若实体实现 ISoftDelete，则软删除（设置 IsDeleted = true）；否则物理删除。
    /// </summary>
    public override async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity is ISoftDelete softDeleteEntity)
        {
            ApplySoftDelete(softDeleteEntity);
            _dbSet.Update(entity);
            await Task.CompletedTask;
        }
        else
        {
            await base.DeleteAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// 批量删除。同样按实体是否实现 ISoftDelete 分流，避免绕过软删除直接物理删除。
    /// </summary>
    public override async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var list = entities as IList<TEntity> ?? entities.ToList();
        var softDeletes = new List<TEntity>();
        var physicalDeletes = new List<TEntity>();

        foreach (var entity in list)
        {
            if (entity is ISoftDelete softDeleteEntity)
            {
                ApplySoftDelete(softDeleteEntity);
                softDeletes.Add(entity);
            }
            else
            {
                physicalDeletes.Add(entity);
            }
        }

        if (softDeletes.Count > 0)
            _dbSet.UpdateRange(softDeletes);

        if (physicalDeletes.Count > 0)
            await base.DeleteRangeAsync(physicalDeletes, cancellationToken);
    }

    /// <summary>
    /// 恢复软删除实体。仅对实现 ISoftDelete 的实体有效。
    /// </summary>
    /// <returns>是否恢复成功（实体不存在或非软删除实体返回 false）</returns>
    public virtual async Task<Boolean> RestoreAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(BuildIdPredicate(id), cancellationToken);

        if (entity is not ISoftDelete softDeleteEntity)
            return false;

        softDeleteEntity.IsDeleted = false;
        softDeleteEntity.DeletedAt = null;
        softDeleteEntity.DeletedBy = null;
        _dbSet.Update(entity);
        return true;
    }

    private void ApplySoftDelete(ISoftDelete softDeleteEntity)
    {
        softDeleteEntity.IsDeleted = true;
        softDeleteEntity.DeletedAt = DateTime.UtcNow;
        softDeleteEntity.DeletedBy ??= _currentUser?.UserId ?? _currentUser?.UserName;
    }

    private static Expression<Func<TEntity, Boolean>> BuildIdPredicate(TKey id)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id, typeof(TKey));
        var equal = Expression.Equal(property, constant);
        return Expression.Lambda<Func<TEntity, Boolean>>(equal, parameter);
    }
}
