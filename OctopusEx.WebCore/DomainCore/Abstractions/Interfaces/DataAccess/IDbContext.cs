using System;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 数据库上下文抽象接口
/// </summary>
public interface IDbContext : IDisposable
{
    /// <summary>
    /// 获取指定实体类型的DbSet
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    IDbSet<TEntity> Set<TEntity>() where TEntity : class;

    /// <summary>
    /// 异步保存所有更改到数据库
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 开始事务
    /// </summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
