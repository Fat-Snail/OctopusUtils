namespace OctopusEx.WebCore.Tests.Idempotency;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OctopusEx.WebCore.Idempotency;

public class IdempotencyMiddlewareTests
{
    private static TestHttpContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MwTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new MwTestDbContext(options);
        ctx.Database.EnsureCreated();
        return new TestHttpContext(ctx);
    }

    [Fact]
    public async Task Middleware_PassesThrough_WhenNoKeyHeader()
    {
        var tc = CreateContext();
        var store = new EFIdempotencyStore(tc.DbContext, new IdempotencyOptions());
        var middleware = new IdempotencyMiddleware(
            _ => Task.CompletedTask,
            store,
            new IdempotencyOptions(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(tc.HttpContext);

        tc.HttpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Middleware_PassesThrough_ForGetMethod()
    {
        var tc = CreateContext();
        tc.HttpContext.Request.Method = "GET";
        tc.HttpContext.Request.Headers["Idempotency-Key"] = "key-1";
        var store = new EFIdempotencyStore(tc.DbContext, new IdempotencyOptions());
        var middleware = new IdempotencyMiddleware(
            _ => Task.CompletedTask,
            store,
            new IdempotencyOptions(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(tc.HttpContext);

        tc.HttpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Middleware_FirstRequest_ProcessesNormally()
    {
        var tc = CreateContext();
        tc.HttpContext.Request.Method = "POST";
        tc.HttpContext.Request.Headers["Idempotency-Key"] = "key-2";
        var store = new EFIdempotencyStore(tc.DbContext, new IdempotencyOptions());
        var middleware = new IdempotencyMiddleware(
            async ctx =>
            {
                ctx.Response.StatusCode = 201;
                await ctx.Response.WriteAsync("""{"id":42}""");
            },
            store,
            new IdempotencyOptions(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(tc.HttpContext);

        tc.HttpContext.Response.StatusCode.Should().Be(201);
        tc.DbContext.Set<IdempotencyKeyEntity>().Should().ContainSingle();
    }

    [Fact]
    public async Task Middleware_DuplicateRequest_Returns409_WhenNoCachedResult()
    {
        var tc = CreateContext();
        tc.HttpContext.Request.Method = "POST";
        tc.HttpContext.Request.Headers["Idempotency-Key"] = "key-3";
        var store = new EFIdempotencyStore(tc.DbContext, new IdempotencyOptions());

        await store.TryAcquireAsync(new IdempotencyRecord
        {
            Key = "key-3",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });

        var middleware = new IdempotencyMiddleware(
            _ => throw new InvalidOperationException("Should not reach next"),
            store,
            new IdempotencyOptions(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(tc.HttpContext);

        tc.HttpContext.Response.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Middleware_DuplicateRequest_ReturnsCachedResult_WhenAvailable()
    {
        var tc = CreateContext();
        tc.HttpContext.Request.Method = "POST";
        tc.HttpContext.Request.Headers["Idempotency-Key"] = "key-4";
        var store = new EFIdempotencyStore(tc.DbContext, new IdempotencyOptions());

        await store.TryAcquireAsync(new IdempotencyRecord
        {
            Key = "key-4",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await store.SetResultAsync("key-4", 200, """{"id":99}""");

        var middleware = new IdempotencyMiddleware(
            _ => throw new InvalidOperationException("Should not reach next"),
            store,
            new IdempotencyOptions(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(tc.HttpContext);

        tc.HttpContext.Response.StatusCode.Should().Be(200);
    }
}

internal sealed class MwTestDbContext : DbContext
{
    public MwTestDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOctopusIdempotency();
        base.OnModelCreating(modelBuilder);
    }
}

internal sealed class TestHttpContext
{
    public MwTestDbContext DbContext { get; }
    public DefaultHttpContext HttpContext { get; }

    public TestHttpContext(MwTestDbContext dbContext)
    {
        DbContext = dbContext;
        HttpContext = new DefaultHttpContext();
        HttpContext.Request.Method = "POST";
        HttpContext.Request.Path = "/api/test";
        HttpContext.Response.Body = new MemoryStream();
    }
}
