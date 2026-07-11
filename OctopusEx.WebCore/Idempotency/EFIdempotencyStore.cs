namespace OctopusEx.WebCore.Idempotency;

/// <summary>
/// 幂等记录 EF Core 实体。映射到 <c>idempotency_keys</c> 表。
/// </summary>
public class IdempotencyKeyEntity
{
    public String Key { get; set; } = "";
    public String? EntityType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public String? ResultCache { get; set; }
    public Int32? StatusCode { get; set; }
}

/// <summary>
/// EF Core 幂等存储实现。
/// </summary>
public class EFIdempotencyStore : IIdempotencyStore
{
    private readonly DbContext _dbContext;
    private readonly DbSet<IdempotencyKeyEntity> _set;
    private readonly IdempotencyOptions _options;

    public EFIdempotencyStore(DbContext dbContext, IdempotencyOptions options)
    {
        _dbContext = dbContext;
        _set = dbContext.Set<IdempotencyKeyEntity>();
        _options = options;
    }

    public async Task<IdempotencyRecord?> TryAcquireAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
    {
        var existing = await _set.FindAsync(new Object[] { record.Key }, cancellationToken);
        if (existing != null && DateTimeOffset.UtcNow <= existing.ExpiresAt)
        {
            return new IdempotencyRecord
            {
                Key = existing.Key,
                EntityType = existing.EntityType,
                CreatedAt = existing.CreatedAt,
                ExpiresAt = existing.ExpiresAt,
                ResultCache = existing.ResultCache,
                StatusCode = existing.StatusCode,
            };
        }

        if (existing != null)
        {
            // 过期了，更新
            existing.ExpiresAt = record.ExpiresAt;
            existing.EntityType = record.EntityType;
            existing.ResultCache = null;
            existing.StatusCode = null;
        }
        else
        {
            _set.Add(new IdempotencyKeyEntity
            {
                Key = record.Key,
                EntityType = record.EntityType,
                CreatedAt = record.CreatedAt,
                ExpiresAt = record.ExpiresAt,
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return null; // 首次请求
    }

    public async Task SetResultAsync(String key, Int32 statusCode, String? resultBody, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new Object[] { key }, cancellationToken);
        if (entity != null)
        {
            entity.StatusCode = statusCode;
            entity.ResultCache = resultBody;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IdempotencyRecord?> GetAsync(String key, CancellationToken cancellationToken = default)
    {
        var entity = await _set.FindAsync(new Object[] { key }, cancellationToken);
        if (entity == null || DateTimeOffset.UtcNow > entity.ExpiresAt) return null;

        return new IdempotencyRecord
        {
            Key = entity.Key,
            EntityType = entity.EntityType,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            ResultCache = entity.ResultCache,
            StatusCode = entity.StatusCode,
        };
    }

    public async Task<Int32> CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = _set.Where(e => e.ExpiresAt <= now);
        _set.RemoveRange(expired);
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// EF Core 模型配置扩展。在 DbContext.OnModelCreating 中调用
/// <c>modelBuilder.AddOctopusIdempotency()</c> 即可注册幂等键表。
/// </summary>
public static class IdempotencyModelBuilderExtensions
{
    public static ModelBuilder AddOctopusIdempotency(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyKeyEntity>(b =>
        {
            b.ToTable("idempotency_keys");
            b.HasKey(e => e.Key);
            b.Property(e => e.Key).HasMaxLength(200).IsRequired();
            b.Property(e => e.EntityType).HasMaxLength(200);
            b.Property(e => e.ResultCache).HasColumnType("text");
            b.Property(e => e.ExpiresAt).IsRequired();
            b.Property(e => e.StatusCode);

            // 过期时间索引（用于清理查询）
            b.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");
        });

        return modelBuilder;
    }
}
