using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OctopusEx.WebCore.Repositories.Interfaces;

namespace OctopusEx.WebCore.Repositories.Implementations.Repositories;

/// <summary>
/// 仓储基类（不依赖具体ORM实现）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class where TKey : notnull
{
    protected readonly IDbContext _dbContext;
    protected readonly IDbSet<TEntity> _dbSet;

    protected RepositoryBase(IDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<TEntity>();
    }

    public virtual IQueryable<TEntity> Find()
    {
        return _dbSet.AsQueryable();
    }

    public virtual IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> condition)
    {
        return _dbSet.Where(condition);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if ( idList.Count == 0 )
            return new List<TEntity>();

        // 假设主键属性名为 "Id"
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, "Id");
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TKey));
        var constant = Expression.Constant(idList);
        var containsCall = Expression.Call(containsMethod, constant, property);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter);

        return await _dbSet.ToListAsync(lambda, cancellationToken);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        await Task.CompletedTask;
    }

    public virtual async Task<bool> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if ( entity == null )
            return false;

        await DeleteAsync(entity);
        return true;
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> condition, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(condition, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? condition = null, CancellationToken cancellationToken = default)
    {
        if ( condition == null )
            return await _dbSet.CountAsync(cancellationToken);

        return await _dbSet.CountAsync(condition, cancellationToken);
    }

    public virtual async Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if ( condition != null )
        {
            query = query.Where(condition);
        }

        return await Task.Run(() => query.ToList(), cancellationToken);
    }

    public virtual async Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if ( condition != null )
        {
            query = query.Where(condition);
        }

        if ( orderBy != null )
        {
            query = orderBy(query);
        }

        return await Task.Run(() => query.ToList(), cancellationToken);
    }

    public virtual async Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        // 应用Include - 由于缺少EFCore，暂时忽略
        // foreach (var include in includes)
        // {
        //     query = query.Include(include);
        // }

        if ( condition != null )
        {
            query = query.Where(condition);
        }

        if ( orderBy != null )
        {
            query = orderBy(query);
        }

        return await Task.Run(() => query.ToList());
    }

    public virtual async Task<List<TEntity>> FindAllAsync(
        Func<IQueryBuilder<TEntity>, IQueryBuilder<TEntity>>? queryBuilder = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        var builder = new QueryBuilder<TEntity>(query);

        if ( queryBuilder != null )
        {
            builder = ( QueryBuilder<TEntity> )queryBuilder(builder);
        }

        var finalQuery = builder.Build();
        return await Task.Run(() => finalQuery.ToList(), cancellationToken);
    }

    public virtual async Task<TEntity?> SingleAsync(Expression<Func<TEntity, bool>> condition, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(condition, cancellationToken);
    }

    /// <summary>
    /// 获取基础查询（用于扩展）
    /// </summary>
    protected IQueryable<TEntity> GetBaseQuery()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        // 如果有需要清理的资源，可以在这里添加
        // 注意：DbContext的生命周期通常由工作单元管理，这里不需要处置
        GC.SuppressFinalize(this);
    }

    // 内部查询构建器类（不依赖EFCore）
    private class QueryBuilder<T> : IQueryBuilder<T> where T : class
    {
        private IQueryable<T> _query;
        private readonly List<Expression<Func<T, object>>> _includes = new();
        private bool _asNoTracking = false;

        public QueryBuilder(IQueryable<T> query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
        {
            if ( predicate != null )
            {
                _query = _query.Where(predicate);
            }
            return this;
        }

        public IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _query = _query.OrderBy(keySelector);
            return this;
        }

        public IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            _query = _query.OrderByDescending(keySelector);
            return this;
        }

        public IQueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if ( _query is IOrderedQueryable<T> orderedQuery )
            {
                _query = orderedQuery.ThenBy(keySelector);
            }
            else
            {
                _query = _query.OrderBy(keySelector);
            }
            return this;
        }

        public IQueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if ( _query is IOrderedQueryable<T> orderedQuery )
            {
                _query = orderedQuery.ThenByDescending(keySelector);
            }
            else
            {
                _query = _query.OrderByDescending(keySelector);
            }
            return this;
        }

        public IQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
        {
            if ( navigationPropertyPath != null )
            {
                // 对于引用类型，直接使用body，不添加Convert节点
                if ( typeof(TProperty).IsValueType )
                {
                    // 值类型需要装箱
                    _includes.Add(Expression.Lambda<Func<T, object>>(
                        Expression.Convert(navigationPropertyPath.Body, typeof(object)),
                        navigationPropertyPath.Parameters));
                }
                else
                {
                    // 引用类型可以直接赋值给object
                    _includes.Add(Expression.Lambda<Func<T, object>>(
                        navigationPropertyPath.Body,
                        navigationPropertyPath.Parameters));
                }
            }
            return this;
        }

        public IQueryBuilder<T> Include(params Expression<Func<T, object>>[] navigationPropertyPaths)
        {
            if ( navigationPropertyPaths != null )
            {
                _includes.AddRange(navigationPropertyPaths);
            }
            return this;
        }

        public IQueryBuilder<T> Take(int limit)
        {
            _query = _query.Take(limit);
            return this;
        }

        public IQueryBuilder<T> Skip(int count)
        {
            _query = _query.Skip(count);
            return this;
        }

        public IQueryBuilder<T> AsNoTracking()
        {
            _asNoTracking = true;
            return this;
        }

        public IQueryable<T> Build()
        {
            var query = _query;

            // 应用Include - 由于缺少EFCore，这里无法实际实现Include功能
            // 保留逻辑但不实际应用，具体实现应由具体ORM提供
            // foreach (var include in _includes)
            // {
            //     query = query.Include(include);
            // }

            // 应用AsNoTracking - 由于缺少EFCore，这里无法实际实现无跟踪查询
            // if (_asNoTracking)
            // {
            //     query = query.AsNoTracking();
            // }

            return query;
        }
    }
}
