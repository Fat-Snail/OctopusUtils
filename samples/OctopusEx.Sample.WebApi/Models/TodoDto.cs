namespace OctopusEx.Sample.WebApi.Models;

/// <summary>
/// TodoItem 的 DTO（由 Mapster 从实体自动映射）
/// </summary>
public class TodoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? TenantId { get; set; }
}

/// <summary>
/// 创建 Todo 的请求 DTO
/// </summary>
public class CreateTodoRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// 更新 Todo 的请求 DTO
/// </summary>
public class UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsCompleted { get; set; }
}
