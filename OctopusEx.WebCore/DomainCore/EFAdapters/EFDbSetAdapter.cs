namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;

public class EFDbSetAdapter<TEntity> : IDbSet<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    public EFDbSetAdapter(DbSet<TEntity> dbSet)
    {
        _dbSet = dbSet ?? throw new ArgumentNullException(nameof(dbSet));
    }

    public Type ElementType => (( IQueryable<TEntity> )_dbSet).ElementType;

    public Expression Expression => (( IQueryable<TEntity> )_dbSet).Expression;

    public IQueryProvider Provider => (( IQueryable<TEntity> )_dbSet).Provider;

    public void Add(TEntity entity) => _dbSet.Add(entity);

    public void AddRange(IEnumerable<TEntity> entities) => _dbSet.AddRange(entities);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => _dbSet.AddAsync(entity, cancellationToken).AsTask();

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => _dbSet.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

    public void Remove(TEntity entity) => _dbSet.Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);

    public Task<TEntity?> FindAsync(params object?[]? keyValues)
        => _dbSet.FindAsync(keyValues).AsTask();

    public Task<TEntity?> FindAsync(object?[]? keyValues, CancellationToken cancellationToken)
        => _dbSet.FindAsync(keyValues, cancellationToken).AsTask();

    public Task<List<TEntity>> ToListAsync(CancellationToken cancellationToken = default)
        => _dbSet.ToListAsync(cancellationToken);

    public Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => _dbSet.AnyAsync(cancellationToken);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => _dbSet.AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
        => _dbSet.CountAsync(cancellationToken);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => _dbSet.CountAsync(predicate, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        => _dbSet.FirstOrDefaultAsync(cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
        => _dbSet.SingleOrDefaultAsync(cancellationToken);

    public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => _dbSet.SingleOrDefaultAsync(predicate, cancellationToken);

    public IQueryable<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        => _dbSet.Include(navigationPropertyPath);

    public IQueryable<TEntity> AsNoTracking()
        => _dbSet.AsNoTracking();

    public IEnumerator<TEntity> GetEnumerator() => (( IEnumerable<TEntity> )_dbSet).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
        (( System.Collections.IEnumerable )_dbSet).GetEnumerator();
}
