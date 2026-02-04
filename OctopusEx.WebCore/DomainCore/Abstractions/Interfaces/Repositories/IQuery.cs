using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 查询接口（读写分离 - 读操作）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IQuery<TEntity, TKey> : IDisposable where TEntity : class
{
    /// <summary>
    /// 获取基础查询对象
    /// </summary>
    IQueryable<TEntity> Find();

    /// <summary>
    /// 条件查询
    /// </summary>
    /// <param name="condition">查询条件</param>
    IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> condition);

    /// <summary>
    /// 获取所有实体（异步）
    /// </summary>
    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键查找实体（异步）
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过标识列表查找实体列表（异步）
    /// </summary>
    /// <param name="ids">标识列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<List<TEntity>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询方法 - 基础版本
    /// </summary>
    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询方法 - 带排序
    /// </summary>
    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询方法 - 带排序和关联加载
    /// </summary>
    Task<List<TEntity>> FindAllAsync(
        Expression<Func<TEntity, bool>>? condition = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// 查询构建器模式
    /// </summary>
    Task<List<TEntity>> FindAllAsync(
        Func<IQueryBuilder<TEntity>, IQueryBuilder<TEntity>>? queryBuilder = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找单个实体（异步）
    /// </summary>
    /// <param name="condition">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TEntity?> SingleAsync(Expression<Func<TEntity, bool>> condition, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否存在满足条件的实体（异步）
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> condition, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计满足条件的实体数量（异步）
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? condition = null, CancellationToken cancellationToken = default);
}
