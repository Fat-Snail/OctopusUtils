namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// EF Core 模型配置扩展。在 DbContext.OnModelCreating 中调用
/// <c>modelBuilder.AddOctopusOutbox()</c> 即可自动注册 outbox_messages 表结构、索引和乐观并发。
/// </summary>
public static class OutboxModelBuilderExtensions
{
    /// <summary>
    /// 为当前 DbContext 注册 Outbox 表 <c>outbox_messages</c>。
    /// 自动配置：
    /// - 主键 Id (Guid)
    /// - 乐观并发 RowVersion
    /// - 索引 IX_OutboxMessages_Pending（按 CreatedAt/NextRetry 过滤未处理消息）
    /// </summary>
    public static ModelBuilder AddOctopusOutbox(this ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(e => e.Id);

            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.EventType).HasMaxLength(500).IsRequired();
            b.Property(e => e.Payload).HasColumnType("text").IsRequired();
            b.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
            b.Property(e => e.ProcessedAt);
            b.Property(e => e.AttemptCount).HasDefaultValue(0);
            b.Property(e => e.LastError).HasMaxLength(2000);
            b.Property(e => e.NextRetry);

            // 乐观并发——SQL Server 用 RowVersion (timestamp)，PostgreSQL 可用 xmin
            b.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnName("row_version");

            // 复合索引：快速查找待处理消息
            b.HasIndex(e => new { e.ProcessedAt, e.AttemptCount, e.NextRetry })
                .HasDatabaseName("IX_OutboxMessages_Pending")
                .HasFilter("ProcessedAt IS NULL");
        });

        return modelBuilder;
    }
}
