namespace OctopusEx.WebCore.Tests.Idempotency;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OctopusEx.WebCore.Events;
using OctopusEx.WebCore.Idempotency;

public class IdempotentEventHandlerTests
{
    internal static Int32 _callCount;

    public IdempotentEventHandlerTests()
    {
        _callCount = 0;
    }

    private static InMemoryEventBus CreateBus()
    {
        var options = new DbContextOptionsBuilder<EhTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new EhTestDbContext(options);
        db.Database.EnsureCreated();

        var store = new EFIdempotencyStore(db, new IdempotencyOptions());

        var services = new ServiceCollection();
        services.AddScoped<IEventHandler<TestEvent>, CountingHandler>();
        var provider = services.BuildServiceProvider();

        return new InMemoryEventBus(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new InMemoryDeadLetterStore(),
            NullLogger<InMemoryEventBus>.Instance,
            store);
    }

    [Fact]
    public async Task FirstCall_ProcessesEvent()
    {
        var bus = CreateBus();
        await bus.PublishAsync(new TestEvent());
        _callCount.Should().Be(1);
    }

    [Fact]
    public async Task DuplicateEvent_SkipsSecondCall()
    {
        var bus = CreateBus();
        var ev = new TestEvent();
        await bus.PublishAsync(ev);
        await bus.PublishAsync(ev);
        _callCount.Should().Be(1);
    }

    [Fact]
    public async Task DifferentEvents_BothProcessed()
    {
        var bus = CreateBus();
        await bus.PublishAsync(new TestEvent());
        await bus.PublishAsync(new TestEvent());
        _callCount.Should().Be(2);
    }
}

internal sealed class TestEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

[Idempotent(TtlSeconds = 1800)]
internal sealed class CountingHandler : IEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref IdempotentEventHandlerTests._callCount);
        return Task.CompletedTask;
    }
}

internal sealed class EhTestDbContext : DbContext
{
    public EhTestDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOctopusIdempotency();
        base.OnModelCreating(modelBuilder);
    }
}
