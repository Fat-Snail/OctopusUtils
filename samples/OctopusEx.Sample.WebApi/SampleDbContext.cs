using Microsoft.EntityFrameworkCore;
using OctopusEx.Sample.WebApi.Models;
using OctopusEx.WebCore.Events.Outbox;

namespace OctopusEx.Sample.WebApi;

/// <summary>
/// 示例 DbContext。演示多租户全局过滤器 + 软删除 + Outbox 表。
/// </summary>
public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options) { }

    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsDeleted);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        });

        // v1.5.4+ 注册 Outbox 表
        modelBuilder.AddOctopusOutbox();
    }
}
