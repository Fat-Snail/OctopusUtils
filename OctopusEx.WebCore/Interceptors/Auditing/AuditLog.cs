namespace OctopusEx.WebCore.Interceptors.Auditing;

public class AuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string DomainName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // INSERT, UPDATE, DELETE
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // JSON格式存储变更详情
    public string Changes { get; set; } = string.Empty;

    // 原始值（用于DELETE操作）
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
