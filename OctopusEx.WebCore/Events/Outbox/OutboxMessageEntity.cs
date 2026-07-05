namespace OctopusEx.WebCore.Events.Outbox;

/// <summary>
/// Outbox 消息 EF Core 实体。映射到 <c>outbox_messages</c> 表。
/// 用户 DbContext 调用 <c>modelBuilder.AddOctopusOutbox()</c> 即可接入。
/// </summary>
public class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public String EventType { get; set; } = "";
    public String Payload { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public Int32 AttemptCount { get; set; }
    public String? LastError { get; set; }
    public DateTimeOffset? NextRetry { get; set; }

    /// <summary>乐观并发令牌。EF Core 自动维护。</summary>
    public Byte[] RowVersion { get; set; } = [];

    /// <summary>转换为领域模型 <see cref="OutboxMessage"/>.</summary>
    public OutboxMessage ToDomain() => new()
    {
        Id = Id,
        EventType = EventType,
        Payload = Payload,
        CreatedAt = CreatedAt,
        ProcessedAt = ProcessedAt,
        AttemptCount = AttemptCount,
        LastError = LastError,
        NextRetry = NextRetry,
    };
}
