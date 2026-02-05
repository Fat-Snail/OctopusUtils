namespace OctopusEx.WebCore.DomainCore.EFAdapters;

using Microsoft.EntityFrameworkCore.Storage;
using Repositories.Interfaces;

public class EFTransactionAdapter : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EFTransactionAdapter(IDbContextTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => _transaction.RollbackAsync(cancellationToken);

    public void Dispose() => _transaction.Dispose();
}
