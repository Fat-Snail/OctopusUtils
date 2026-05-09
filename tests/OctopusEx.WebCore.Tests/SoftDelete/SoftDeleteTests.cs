namespace OctopusEx.WebCore.Tests.SoftDelete;

using Microsoft.EntityFrameworkCore;
using OctopusEx.WebCore.DomainCore.EFAdapters;
using OctopusEx.WebCore.DomainCore.SoftDelete;
using OctopusEx.WebCore.Helpers;
using OctopusEx.WebCore.Repositories.Interfaces;

public class SoftDeleteTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly EFContextAdapter _adapter;

    public SoftDeleteTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestDbContext(options);
        _adapter = new EFContextAdapter(_dbContext);
    }

    public void Dispose() => _dbContext.Dispose();

    private EFRepository<TUser, Int32> NewRepo(ICurrentUser? user = null)
        => user == null
            ? new EFRepository<TUser, Int32>(_adapter)
            : new EFRepository<TUser, Int32>(_adapter, user);

    [Fact]
    public async Task DeleteAsync_OnSoftDeleteEntity_MarksDeletedNotPhysicallyRemoved()
    {
        var user = new TUser { Id = 1, Name = "alice" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var repo = NewRepo();
        await repo.DeleteAsync(user);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // 默认查询过滤掉
        (await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == 1)).Should().BeNull();
        // IgnoreQueryFilters 仍能查到，且 IsDeleted = true
        var raw = _dbContext.Users.IgnoreQueryFilters().Single(u => u.Id == 1);
        raw.IsDeleted.Should().BeTrue();
        raw.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteRangeAsync_OnSoftDeleteEntities_MarksAllDeleted()
    {
        var users = new[]
        {
            new TUser { Id = 1, Name = "a" },
            new TUser { Id = 2, Name = "b" },
            new TUser { Id = 3, Name = "c" },
        };
        _dbContext.Users.AddRange(users);
        await _dbContext.SaveChangesAsync();

        var repo = NewRepo();
        await repo.DeleteRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        var deleted = _dbContext.Users.IgnoreQueryFilters().Count(u => u.IsDeleted);
        deleted.Should().Be(3);
        _dbContext.Users.Count().Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_AutoFillsDeletedByFromCurrentUser()
    {
        var user = new TUser { Id = 1, Name = "alice" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var currentUser = new FixedCurrentUser("admin-id", "admin");
        var repo = NewRepo(currentUser);

        await repo.DeleteAsync(user);
        await _dbContext.SaveChangesAsync();

        var raw = _dbContext.Users.IgnoreQueryFilters().Single(u => u.Id == 1);
        raw.DeletedBy.Should().Be("admin-id");
    }

    [Fact]
    public async Task RestoreAsync_OnSoftDeletedEntity_ResetsDeletedFlags()
    {
        var user = new TUser { Id = 1, Name = "alice", IsDeleted = true, DeletedAt = DateTime.UtcNow, DeletedBy = "x" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var repo = NewRepo();
        var ok = await repo.RestoreAsync(1);
        await _dbContext.SaveChangesAsync();

        ok.Should().BeTrue();
        var restored = await _dbContext.Users.FindAsync(1);
        restored.Should().NotBeNull();
        restored!.IsDeleted.Should().BeFalse();
        restored.DeletedAt.Should().BeNull();
        restored.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_OnNonExistingId_ReturnsFalse()
    {
        var repo = NewRepo();
        var ok = await repo.RestoreAsync(999);
        ok.Should().BeFalse();
    }

    public class TUser : ISoftDelete
    {
        public Int32 Id { get; set; }
        public String Name { get; set; } = "";
        public Boolean IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public String? DeletedBy { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TUser> Users => Set<TUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddSoftDeleteFilter();
        }
    }

    private sealed class FixedCurrentUser : ICurrentUser
    {
        public FixedCurrentUser(String? id, String? name)
        {
            UserId = id;
            UserName = name;
            IsAuthenticated = id != null;
        }
        public String? UserId { get; }
        public String? UserName { get; }
        public Boolean IsAuthenticated { get; }
    }

    private sealed class EFContextAdapter : IDbContext
    {
        private readonly TestDbContext _ctx;
        public EFContextAdapter(TestDbContext ctx) => _ctx = ctx;

        public IDbSet<TEntity> Set<TEntity>() where TEntity : class
            => new EFDbSetAdapter<TEntity>(_ctx.Set<TEntity>());

        public Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _ctx.SaveChangesAsync(cancellationToken);

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void Dispose() => _ctx.Dispose();
    }
}
