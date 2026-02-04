using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 数据库集合接口，提供对实体集合的访问和操作
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IDbSet<TEntity> : IQueryable<TEntity> where TEntity : class
{
    /// <summary>
    /// 添加实体
    /// </summary>
    void Add(TEntity entity);

    /// <summary>
    /// 添加多个实体
    /// </summary>
    void AddRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 异步添加实体
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步添加多个实体
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// 更新多个实体
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 删除实体
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// 删除多个实体
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 根据主键查找实体（异步）
    /// </summary>
    Task<TEntity?> FindAsync(params object?[]? keyValues);

    /// <summary>
    /// 根据主键查找实体（异步）
    /// </summary>
    Task<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken);

    /// <summary>
    /// 异步返回列表
    /// </summary>
    Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步返回满足条件的实体列表
    /// </summary>
    Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步判断是否存在任何实体
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步判断是否存在满足条件的实体
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步计算实体总数
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步计算满足条件的实体数量
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步返回第一个实体，如果没有则返回默认值
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步返回第一个满足条件的实体，如果没有则返回默认值
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步返回唯一实体，如果没有或超过一个则返回默认值
    /// </summary>
    Task<TEntity?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步返回唯一满足条件的实体，如果没有或超过一个则返回默认值
    /// </summary>
    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 包含关联实体
    /// </summary>
    IQueryable<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath);

    /// <summary>
    /// 启用无跟踪查询
    /// </summary>
    IQueryable<TEntity> AsNoTracking();
}
