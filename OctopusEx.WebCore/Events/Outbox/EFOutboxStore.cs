namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// EF Core Outbox 存储实现。使用与业务实体相同的 DbContext，保证事务一致性。
/// 支持与 SQL Server / PostgreSQL / SQLite 等后端。
/// </summary>
public class EFOutboxStore : IOutboxStore
{
    private readonly DbContext _dbContext;
    private readonly DbSet<OutboxMessageEntity> _set;
    private readonly ILogger<EFOutboxStore>? _logger;
    private readonly String _provider;

    public EFOutboxStore(DbContext dbContext, ILogger<EFOutboxStore>? logger = null)
    {
        _dbContext = dbContext;
        _set = dbContext.Set<OutboxMessageEntity>();
        _logger = logger;
        // 探测数据库提供程序（用于选择正确的锁定语法）
        _provider = dbContext.Database.ProviderName ?? "";
    }

    public async Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _set.AddAsync(new OutboxMessageEntity
        {
            Id = message.Id,
            EventType = message.EventType,
            Payload = message.Payload,
            CreatedAt = message.CreatedAt,
        }, cancellationToken);
        // 注意：实际落库由调用方的 SaveChanges（同一事务）完成。
    }

    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(Int32 batchSize, Int32 maxAttempts, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // 根据数据库提供程序选择合适的锁定提示
        // PostgreSQL: FOR UPDATE SKIP LOCKED
        // SQL Server: WITH (READPAST, UPDLOCK)
        // SQLite / 其他: 无锁定（单写场景安全）

        var query = _set
            .AsQueryable()
            .Where(e => e.ProcessedAt == null
                     && e.AttemptCount < maxAttempts
                     && (e.NextRetry == null || e.NextRetry <= now))
            .OrderBy(e => e.NextRetry ?? e.CreatedAt)
            .Take(batchSize);

        // 对于 PG / SQL Server，使用原始 SQL 锁定提示避免多 dispatcher 冲突
        IEnumerable<OutboxMessageEntity> entities;
        if (_provider.Contains("Npgsql") || _provider.Contains("PostgreSQL"))
        {
            // PostgreSQL: FOR UPDATE SKIP LOCKED
            entities = await FetchWithPgSkipLockedAsync(query, cancellationToken);
        }
        else if (_provider.Contains("Microsoft.EntityFrameworkCore.SqlServer"))
        {
            // SQL Server: 使用 FromSqlRaw + READPAST, UPDLOCK
            entities = await FetchWithSqlServerHintsAsync(maxAttempts, now, batchSize, cancellationToken);
        }
        else
        {
            // 无锁定（SQLite、InMemory、开发环境）
            entities = await query.ToListAsync(cancellationToken);
        }

        return entities.Select(e => e.ToDomain()).ToList();
    }

    private async Task<IEnumerable<OutboxMessageEntity>> FetchWithPgSkipLockedAsync(IQueryable<OutboxMessageEntity> query, CancellationToken ct)
    {
        // Npgsql 支持 FOR UPDATE SKIP LOCKED 通过 FromSqlRaw
        var ids = await query.Select(e => e.Id).ToListAsync(ct);
        if (ids.Count == 0) return [];

        var idList = String.Join(",", ids.Select(i => $"'{i}'"));
        var sql = $"SELECT * FROM outbox_messages WHERE Id IN ({idList}) ORDER BY COALESCE(\"NextRetry\", \"CreatedAt\") FOR UPDATE SKIP LOCKED";

        var entities = await _set.FromSqlRaw(sql).ToListAsync(ct);

        // 按原始查询的排序顺序返回
        return entities.OrderBy(e => e.NextRetry ?? e.CreatedAt).Take(ids.Count);
    }

    private async Task<IEnumerable<OutboxMessageEntity>> FetchWithSqlServerHintsAsync(Int32 maxAttempts, DateTimeOffset now, Int32 batchSize, CancellationToken ct)
    {
        var sql = @"SELECT TOP ({0}) * FROM [outbox_messages]
                     WITH (READPAST, UPDLOCK, ROWLOCK)
                     WHERE [ProcessedAt] IS NULL AND [AttemptCount] < {1}
                       AND ([NextRetry] IS NULL OR [NextRetry] <= '{2}')
                     ORDER BY COALESCE([NextRetry], [CreatedAt])";

        sql = String.Format(sql, batchSize, maxAttempts, now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        return await _set.FromSqlRaw(sql).ToListAsync(ct);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new Object[] { messageId }, cancellationToken);
        if (entity != null)
        {
            entity.ProcessedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkFailedAsync(Guid messageId, String error, CancellationToken cancellationToken = default)
        => await MarkFailedAsync(messageId, error, RetryStrategy.ExponentialWithJitter, TimeSpan.FromSeconds(30), cancellationToken);

    public async Task MarkFailedAsync(Guid messageId, String error, RetryStrategy retryStrategy, TimeSpan retryInterval, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new Object[] { messageId }, cancellationToken);
        if (entity != null)
        {
            entity.AttemptCount++;
            entity.LastError = Truncate(error, 2000);
            entity.NextRetry = CalculateNextRetry(entity.AttemptCount, retryStrategy, retryInterval);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static String Truncate(String value, Int32 maxLength)
        => value.Length <= maxLength ? value : value.Substring(0, maxLength);

    private static DateTimeOffset CalculateNextRetry(Int32 attemptCount, RetryStrategy strategy, TimeSpan baseInterval) =>
        strategy switch
        {
            RetryStrategy.Linear => DateTimeOffset.UtcNow + baseInterval * attemptCount,
            RetryStrategy.Exponential => DateTimeOffset.UtcNow + baseInterval * (Int64)Math.Pow(2, attemptCount - 1),
            RetryStrategy.ExponentialWithJitter => DateTimeOffset.UtcNow + TimeSpan.FromTicks((Int64)(baseInterval.Ticks * Math.Pow(2, attemptCount - 1) * Random.Shared.NextDouble())),
            _ => DateTimeOffset.UtcNow + baseInterval * (Int64)Math.Pow(2, attemptCount - 1),
        };
}
