using Mapster;
using Microsoft.EntityFrameworkCore;
using OctopusEx.Sample.WebApi.Models;
using OctopusEx.WebCore.Events;

namespace OctopusEx.Sample.WebApi;

/// <summary>
/// 待办事项业务服务。演示 Mapster 映射 + 领域事件发布 + 软删除。
/// </summary>
public class TodoService
{
    private readonly SampleDbContext _db;
    private readonly IDomainEventCollector _eventCollector;

    public TodoService(SampleDbContext db, IDomainEventCollector eventCollector)
    {
        _db = db;
        _eventCollector = eventCollector;
    }

    public async Task<List<TodoDto>> GetAllAsync()
    {
        var items = await _db.Todos
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedAt)
            .ProjectToType<TodoDto>()
            .ToListAsync();
        return items;
    }

    public async Task<TodoDto?> GetByIdAsync(Guid id)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item == null || item.IsDeleted) return null;
        return item.Adapt<TodoDto>();
    }

    public async Task<TodoDto> CreateAsync(CreateTodoRequest request, string? tenantId)
    {
        var item = new TodoItem
        {
            Title = request.Title,
            Description = request.Description,
            TenantId = tenantId
        };

        _db.Todos.Add(item);

        // 收集领域事件（事务提交后由 Outbox 派发）
        _eventCollector.Raise(new TodoCreatedEvent(item.Id, item.Title));

        await _db.SaveChangesAsync();
        return item.Adapt<TodoDto>();
    }

    public async Task<TodoDto?> UpdateAsync(Guid id, UpdateTodoRequest request)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item == null || item.IsDeleted) return null;

        if (request.Title != null) item.Title = request.Title;
        if (request.Description != null) item.Description = request.Description;
        if (request.IsCompleted.HasValue) item.IsCompleted = request.IsCompleted.Value;

        await _db.SaveChangesAsync();
        return item.Adapt<TodoDto>();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _db.Todos.FindAsync(id);
        if (item == null || item.IsDeleted) return false;

        // 软删除
        item.IsDeleted = true;
        item.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }
}
