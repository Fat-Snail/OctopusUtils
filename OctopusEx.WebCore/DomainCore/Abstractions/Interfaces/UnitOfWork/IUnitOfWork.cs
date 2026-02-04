using System;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 工作单元接口
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 获取指定实体类型的仓储
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class where TKey : notnull;

    /// <summary>
    /// 保存所有更改（异步）
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始事务
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行事务操作
    /// </summary>
    Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
