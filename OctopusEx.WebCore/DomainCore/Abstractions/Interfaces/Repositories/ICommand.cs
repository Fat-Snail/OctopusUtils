using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 命令接口（读写分离 - 写操作）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface ICommand<TEntity, TKey> : IDisposable where TEntity : class
{
    /// <summary>
    /// 添加实体（异步）
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加多个实体（异步）
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（异步）
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新多个实体（异步）
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体（异步）
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除多个实体（异步）
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键删除实体（异步）
    /// </summary>
    Task<bool> DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
}
