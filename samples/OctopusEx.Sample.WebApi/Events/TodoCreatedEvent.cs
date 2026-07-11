using OctopusEx.WebCore.Events;

namespace OctopusEx.Sample.WebApi;

/// <summary>
/// 待办事项创建事件。
/// </summary>
public class TodoCreatedEvent : DomainEventBase
{
    public Guid TodoId { get; }
    public string Title { get; }

    public TodoCreatedEvent(Guid todoId, string title)
    {
        TodoId = todoId;
        Title = title;
    }
}

/// <summary>
/// TodoCreatedEvent 的处理程序。
/// </summary>
public class TodoCreatedEventHandler : IEventHandler<TodoCreatedEvent>
{
    private readonly ILogger<TodoCreatedEventHandler> _logger;

    public TodoCreatedEventHandler(ILogger<TodoCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TodoCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Todo created: Id={TodoId}, Title={Title}, EventId={EventId}",
            @event.TodoId, @event.Title, @event.EventId);
        return Task.CompletedTask;
    }
}
