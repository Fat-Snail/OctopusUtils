using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OctopusEx.WebCore.Repositories.Interfaces;

namespace OctopusEx.WebCore.Repositories.Implementations.UnitOfWork;

/// <summary>
/// 工作单元基类，提供通用的工作单元实现
/// </summary>
public abstract class UnitOfWorkBase : IUnitOfWork
{
    private readonly IDbContext _dbContext;
    private ITransaction? _currentTransaction;
    private readonly Dictionary<Type, object> _repositories = new();

    protected UnitOfWorkBase(IDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// 获取数据库上下文
    /// </summary>
    protected IDbContext DbContext => _dbContext;

    /// <summary>
    /// 获取指定实体类型的仓储
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    public virtual IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class where TKey : notnull
    {
        var repositoryType = typeof(IRepository<TEntity, TKey>);

        if ( !_repositories.ContainsKey(repositoryType) )
        {
            var repository = CreateRepository<TEntity, TKey>();
            _repositories[repositoryType] = repository;
        }

        return ( IRepository<TEntity, TKey> )_repositories[repositoryType];
    }

    /// <summary>
    /// 创建仓储实例（由子类实现）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    protected abstract IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class where TKey : notnull;

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if ( _currentTransaction != null )
        {
            throw new InvalidOperationException("事务已开始");
        }

        _currentTransaction = await _dbContext.BeginTransactionAsync(cancellationToken);
    }

    public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if ( _currentTransaction == null )
        {
            throw new InvalidOperationException("没有活动的事务");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if ( _currentTransaction == null )
        {
            throw new InvalidOperationException("没有活动的事务");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public virtual async Task ExecuteTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);

        try
        {
            await operation();
            await CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public virtual void Dispose()
    {
        _currentTransaction?.Dispose();
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }
}
