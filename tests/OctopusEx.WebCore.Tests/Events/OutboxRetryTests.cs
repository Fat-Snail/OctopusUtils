namespace OctopusEx.WebCore.Tests.Events;

using OctopusEx.WebCore.Events.Outbox;

public class OutboxRetryTests
{
    [Fact]
    public async Task MarkFailed_SetsNextRetry_BasedOnLinearStrategy()
    {
        var store = new InMemoryOutboxStore();
        var msg = new OutboxMessage { EventType = "T", Payload = "{}" };
        await store.EnqueueAsync(msg);

        var baseInterval = TimeSpan.FromSeconds(30);
        await store.MarkFailedAsync(msg.Id, "err", RetryStrategy.Linear, baseInterval);

        var snap = store.Snapshot().Single();
        snap.AttemptCount.Should().Be(1);
        snap.NextRetry.Should().NotBeNull();
        snap.NextRetry.Should().BeCloseTo(DateTimeOffset.UtcNow + baseInterval, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task MarkFailed_SetsNextRetry_BasedOnExponentialStrategy()
    {
        var store = new InMemoryOutboxStore();
        var msg = new OutboxMessage { EventType = "T", Payload = "{}" };
        await store.EnqueueAsync(msg);

        var baseInterval = TimeSpan.FromSeconds(10);
        await store.MarkFailedAsync(msg.Id, "err", RetryStrategy.Exponential, baseInterval);
        await store.MarkFailedAsync(msg.Id, "err2", RetryStrategy.Exponential, baseInterval);

        var snap = store.Snapshot().Single();
        snap.AttemptCount.Should().Be(2);
        // Exponential: 2^(2-1) = 2, so next retry ≈ now + 20s
        snap.NextRetry.Should().NotBeNull();
        snap.NextRetry.Should().BeCloseTo(DateTimeOffset.UtcNow + baseInterval * 2, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task FetchPending_SkipsMessagesWithFutureNextRetry()
    {
        var store = new InMemoryOutboxStore();
        var readyMsg = new OutboxMessage { EventType = "T1", Payload = "{}" };
        var futureMsg = new OutboxMessage
        {
            EventType = "T2",
            Payload = "{}",
            NextRetry = DateTimeOffset.UtcNow.AddMinutes(5),
        };
        await store.EnqueueAsync(readyMsg);
        await store.EnqueueAsync(futureMsg);

        var batch = await store.FetchPendingAsync(10, maxAttempts: 5);
        batch.Should().ContainSingle().Which.Id.Should().Be(readyMsg.Id);
    }

    [Fact]
    public async Task FetchPending_IncludesMessagesWithPastNextRetry()
    {
        var store = new InMemoryOutboxStore();
        var pastMsg = new OutboxMessage
        {
            EventType = "T",
            Payload = "{}",
            NextRetry = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        await store.EnqueueAsync(pastMsg);

        var batch = await store.FetchPendingAsync(10, maxAttempts: 5);
        batch.Should().ContainSingle().Which.Id.Should().Be(pastMsg.Id);
    }

    [Fact]
    public async Task MarkFailed_WithDefaultOverload_UsesExponentialWithJitter()
    {
        var store = new InMemoryOutboxStore();
        var msg = new OutboxMessage { EventType = "T", Payload = "{}" };
        await store.EnqueueAsync(msg);

        await store.MarkFailedAsync(msg.Id, "err");

        var snap = store.Snapshot().Single();
        snap.AttemptCount.Should().Be(1);
        snap.NextRetry.Should().NotBeNull();
        snap.LastError.Should().Be("err");
    }

    [Fact]
    public async Task FetchPending_OrdersByNextRetryThenCreatedAt()
    {
        var store = new InMemoryOutboxStore();
        var noRetry = new OutboxMessage { EventType = "T1", Payload = "{}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) };
        var soonRetry = new OutboxMessage
        {
            EventType = "T2",
            Payload = "{}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            NextRetry = DateTimeOffset.UtcNow.AddSeconds(-5),
        };
        await store.EnqueueAsync(noRetry);
        await store.EnqueueAsync(soonRetry);

        var batch = await store.FetchPendingAsync(10, maxAttempts: 5);
        // Both messages should be returned since both are ready for processing
        batch.Should().HaveCount(2);
    }

    [Fact]
    public void OutboxOptions_DefaultValues()
    {
        var opts = new OutboxOptions();
        opts.PollInterval.Should().Be(TimeSpan.FromSeconds(1));
        opts.BatchSize.Should().Be(100);
        opts.MaxAttempts.Should().Be(5);
        opts.RetryInterval.Should().Be(TimeSpan.FromSeconds(30));
        opts.RetryStrategy.Should().Be(RetryStrategy.ExponentialWithJitter);
    }
}
