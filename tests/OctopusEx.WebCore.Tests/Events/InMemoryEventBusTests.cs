namespace OctopusEx.WebCore.Tests.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OctopusEx.WebCore.Events;

public class InMemoryEventBusTests
{
    public class OrderPlacedEvent : DomainEventBase
    {
        public String OrderId { get; set; } = "";
    }

    public class TrackingHandler : IEventHandler<OrderPlacedEvent>
    {
        public List<String> Handled { get; } = new();
        public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Handled.Add(@event.OrderId);
            return Task.CompletedTask;
        }
    }

    public class FlakyHandler : IEventHandler<OrderPlacedEvent>
    {
        public Int32 Calls { get; private set; }
        public Int32 FailUntilAttempt { get; set; } = 2;
        public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls < FailUntilAttempt) throw new InvalidOperationException("flake");
            return Task.CompletedTask;
        }
    }

    public class AlwaysFailingHandler : IEventHandler<OrderPlacedEvent>
    {
        public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("permanent");
    }

    private static (IEventBus bus, InMemoryDeadLetterStore dlq, IServiceProvider sp) BuildBus(
        params (Type iface, Object instance)[] handlers)
    {
        var services = new ServiceCollection();
        var dlq = new InMemoryDeadLetterStore();
        services.AddSingleton<IDeadLetterStore>(dlq);
        services.AddSingleton<ILogger<InMemoryEventBus>>(NullLogger<InMemoryEventBus>.Instance);
        services.AddSingleton(new EventBusOptions { MaxRetries = 2, RetryBaseDelayMs = 1 });
        foreach (var (iface, instance) in handlers)
            services.AddSingleton(iface, instance);
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<IEventBus>(), dlq, sp);
    }

    [Fact]
    public async Task PublishAsync_DispatchesToAllHandlers()
    {
        var h1 = new TrackingHandler();
        var h2 = new TrackingHandler();
        var (bus, _, _) = BuildBus(
            (typeof(IEventHandler<OrderPlacedEvent>), h1),
            (typeof(IEventHandler<OrderPlacedEvent>), h2));

        await bus.PublishAsync(new OrderPlacedEvent { OrderId = "A1" });

        h1.Handled.Should().Equal("A1");
        h2.Handled.Should().Equal("A1");
    }

    [Fact]
    public async Task PublishAsync_NoHandler_DoesNotThrow()
    {
        var (bus, _, _) = BuildBus();
        var act = async () => await bus.PublishAsync(new OrderPlacedEvent());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_TransientFailure_RetriesAndSucceeds()
    {
        var flaky = new FlakyHandler { FailUntilAttempt = 2 };
        var (bus, dlq, _) = BuildBus(
            (typeof(IEventHandler<OrderPlacedEvent>), flaky));

        await bus.PublishAsync(new OrderPlacedEvent());

        flaky.Calls.Should().Be(2);
        (await dlq.ListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_PermanentFailure_RecordsToDeadLetterQueue()
    {
        var bad = new AlwaysFailingHandler();
        var (bus, dlq, _) = BuildBus(
            (typeof(IEventHandler<OrderPlacedEvent>), bad));

        var ev = new OrderPlacedEvent();
        await bus.PublishAsync(ev);

        var letters = await dlq.ListAsync();
        letters.Should().HaveCount(1);
        letters[0].EventId.Should().Be(ev.EventId);
        letters[0].HandlerTypeName.Should().Contain(nameof(AlwaysFailingHandler));
    }

    [Fact]
    public async Task PublishAsync_OneHandlerFails_OthersStillRun()
    {
        var ok = new TrackingHandler();
        var bad = new AlwaysFailingHandler();
        var (bus, _, _) = BuildBus(
            (typeof(IEventHandler<OrderPlacedEvent>), ok),
            (typeof(IEventHandler<OrderPlacedEvent>), bad));

        await bus.PublishAsync(new OrderPlacedEvent { OrderId = "X" });

        ok.Handled.Should().Equal("X");
    }

    [Fact]
    public void DomainEventCollector_DrainReturnsAndClears()
    {
        var collector = new DomainEventCollector();
        collector.Raise(new OrderPlacedEvent { OrderId = "1" });
        collector.Raise(new OrderPlacedEvent { OrderId = "2" });

        collector.Drain().Should().HaveCount(2);
        collector.Drain().Should().BeEmpty();
    }
}
