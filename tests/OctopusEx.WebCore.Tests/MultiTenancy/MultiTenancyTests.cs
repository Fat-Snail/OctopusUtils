namespace OctopusEx.WebCore.Tests.MultiTenancy;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OctopusEx.WebCore.MultiTenancy;

public class MultiTenancyTests : IDisposable
{
    // 必须在所有测试间共享：EF Core 按 DbContext 类型缓存编译后的 model，
    // 模型里的全局过滤器表达式会捕获 ICurrentTenant 实例引用 —— 与生产环境
    // singleton 注册保持一致才能正确工作。
    private static readonly CurrentTenant SharedTenant = new();

    private readonly TenantDbContext _ctx;
    private readonly CurrentTenant _currentTenant = SharedTenant;

    public MultiTenancyTests()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _ctx = new TenantDbContext(options, _currentTenant);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public async Task QueryFilter_OnlyReturnsCurrentTenantRows()
    {
        _ctx.Posts.AddRange(
            new Post { Id = 1, TenantId = "t1", Title = "p1" },
            new Post { Id = 2, TenantId = "t2", Title = "p2" },
            new Post { Id = 3, TenantId = "t1", Title = "p3" });
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        using var scope = _currentTenant.Use("t1");

        var posts = await _ctx.Posts.ToListAsync();
        posts.Should().HaveCount(2);
        posts.Should().AllSatisfy(p => p.TenantId.Should().Be("t1"));
    }

    [Fact]
    public async Task QueryFilter_NoTenantContext_ReturnsAll()
    {
        _ctx.Posts.AddRange(
            new Post { Id = 1, TenantId = "t1", Title = "p1" },
            new Post { Id = 2, TenantId = "t2", Title = "p2" });
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();

        // 无租户上下文（_currentTenant.TenantId 默认为 null），过滤器允许所有数据

        var posts = await _ctx.Posts.ToListAsync();
        posts.Should().HaveCount(2);
    }

    [Fact]
    public void CurrentTenant_UseScope_RestoresPreviousValue()
    {
        var ct = new CurrentTenant();
        using (ct.Use("outer"))
        {
            ct.TenantId.Should().Be("outer");
            using (ct.Use("inner"))
            {
                ct.TenantId.Should().Be("inner");
            }
            ct.TenantId.Should().Be("outer");
        }
        ct.TenantId.Should().BeNull();
    }

    [Fact]
    public void HeaderTenantResolver_ReadsFromHeader()
    {
        var resolver = new HeaderTenantResolver();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Tenant-Id"] = "acme";

        resolver.Resolve(ctx).Should().Be("acme");
    }

    [Fact]
    public void QueryTenantResolver_ReadsFromQuery()
    {
        var resolver = new QueryTenantResolver();
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?tenant=acme");

        resolver.Resolve(ctx).Should().Be("acme");
    }

    [Fact]
    public void SubdomainTenantResolver_ExtractsFirstLabel()
    {
        var resolver = new SubdomainTenantResolver();
        var ctx = new DefaultHttpContext();
        ctx.Request.Host = new HostString("acme.example.com");

        resolver.Resolve(ctx).Should().Be("acme");
    }

    [Fact]
    public void CompositeTenantResolver_FallsThroughOnEmpty()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.QueryString = new QueryString("?tenant=fromquery");

        var composite = new CompositeTenantResolver(new ITenantResolver[]
        {
            new HeaderTenantResolver(),    // 不会命中
            new QueryTenantResolver(),     // 命中
        });

        composite.Resolve(ctx).Should().Be("fromquery");
    }

    public class Post : IMultiTenant
    {
        public Int32 Id { get; set; }
        public String TenantId { get; set; } = "";
        public String Title { get; set; } = "";
    }

    public class TenantDbContext : DbContext
    {
        private readonly ICurrentTenant _tenant;
        public DbSet<Post> Posts => Set<Post>();

        public TenantDbContext(DbContextOptions<TenantDbContext> options, ICurrentTenant tenant)
            : base(options) => _tenant = tenant;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // EF Core 推荐的多租户过滤器模式：闭包必须 root 在 DbContext 上才会被
            // ParameterExtractingExpressionVisitor 按查询重新求值。
            // OctopusEx 提供 modelBuilder.HasMultiTenantFilter<T>(_tenant) 辅助方法，
            // 但生产环境通常更推荐直接写下面这种闭包；二者语义等价但 EF 缓存行为更稳定。
            modelBuilder.Entity<Post>().HasQueryFilter(
                p => p.TenantId == _tenant.TenantId || _tenant.TenantId == null);
        }
    }
}
