namespace OctopusEx.WebCore.Tests.Idempotency;

using Microsoft.EntityFrameworkCore;
using OctopusEx.WebCore.Idempotency;

public class IdempotencyStoreTests
{
    private static TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public async Task TryAcquire_FirstCall_ReturnsNull_AndInsertsRecord()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());
        var record = new IdempotencyRecord
        {
            Key = "req-001",
            EntityType = "POST /api/orders",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        var result = await store.TryAcquireAsync(record);
        result.Should().BeNull();

        ctx.Set<IdempotencyKeyEntity>().Should().ContainSingle();
        ctx.Set<IdempotencyKeyEntity>().First().Key.Should().Be("req-001");
    }

    [Fact]
    public async Task TryAcquire_SecondCall_ReturnsExistingRecord()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());
        var record = new IdempotencyRecord
        {
            Key = "req-002",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        await store.TryAcquireAsync(record);
        var result = await store.TryAcquireAsync(record);

        result.Should().NotBeNull();
        result!.Key.Should().Be("req-002");
    }

    [Fact]
    public async Task TryAcquire_ExpiredRecord_AllowsReacquire()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());

        var expiredRecord = new IdempotencyRecord
        {
            Key = "req-003",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
        };

        ctx.Set<IdempotencyKeyEntity>().Add(new IdempotencyKeyEntity
        {
            Key = "req-003",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
        });
        await ctx.SaveChangesAsync();

        var newRecord = new IdempotencyRecord
        {
            Key = "req-003",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        };

        var result = await store.TryAcquireAsync(newRecord);
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetResult_UpdatesRecord()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());

        await store.TryAcquireAsync(new IdempotencyRecord
        {
            Key = "req-004",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });

        await store.SetResultAsync("req-004", 200, """{"id":1}""");

        var entity = ctx.Set<IdempotencyKeyEntity>().First(e => e.Key == "req-004");
        entity.StatusCode.Should().Be(200);
        entity.ResultCache.Should().Be("""{"id":1}""");
    }

    [Fact]
    public async Task GetAsync_ReturnsRecord_WhenExists()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());

        await store.TryAcquireAsync(new IdempotencyRecord
        {
            Key = "req-005",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await store.SetResultAsync("req-005", 201, """{"created":true}""");

        var record = await store.GetAsync("req-005");
        record.Should().NotBeNull();
        record!.StatusCode.Should().Be(201);
        record.ResultCache.Should().Be("""{"created":true}""");
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenExpired()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());

        ctx.Set<IdempotencyKeyEntity>().Add(new IdempotencyKeyEntity
        {
            Key = "req-006",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
        });
        await ctx.SaveChangesAsync();

        var record = await store.GetAsync("req-006");
        record.Should().BeNull();
    }

    [Fact]
    public async Task CleanupExpiredAsync_RemovesExpiredRecords()
    {
        using var ctx = CreateContext();
        var store = new EFIdempotencyStore(ctx, new IdempotencyOptions());

        ctx.Set<IdempotencyKeyEntity>().AddRange(
            new IdempotencyKeyEntity { Key = "expired-1", ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1), CreatedAt = DateTimeOffset.UtcNow.AddHours(-2) },
            new IdempotencyKeyEntity { Key = "expired-2", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5), CreatedAt = DateTimeOffset.UtcNow.AddHours(-1) },
            new IdempotencyKeyEntity { Key = "alive-1", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), CreatedAt = DateTimeOffset.UtcNow }
        );
        await ctx.SaveChangesAsync();

        var removed = await store.CleanupExpiredAsync();
        removed.Should().Be(2);
        ctx.Set<IdempotencyKeyEntity>().Should().ContainSingle();
        ctx.Set<IdempotencyKeyEntity>().First().Key.Should().Be("alive-1");
    }

    [Fact]
    public void IdempotencyOptions_DefaultValues()
    {
        var opts = new IdempotencyOptions();
        opts.DefaultTtl.Should().Be(TimeSpan.FromHours(24));
        opts.CleanupInterval.Should().Be(TimeSpan.FromHours(1));
        opts.HeaderName.Should().Be("Idempotency-Key");
        opts.EnableHttpMiddleware.Should().BeTrue();
        opts.ApplicableMethods.Should().Contain(new[] { "POST", "PUT", "PATCH", "DELETE" });
    }
}

internal sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOctopusIdempotency();
        base.OnModelCreating(modelBuilder);
    }
}
