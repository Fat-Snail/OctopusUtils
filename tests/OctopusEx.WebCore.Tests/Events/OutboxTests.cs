namespace OctopusEx.WebCore.Tests.Events;

using OctopusEx.WebCore.Events;
using OctopusEx.WebCore.Events.Outbox;

public class OutboxTests
{
    [Fact]
    public async Task Enqueue_AndFetchPending_ReturnsByCreationOrder()
    {
        var store = new InMemoryOutboxStore();
        var earlier = new OutboxMessage { EventType = "T1", Payload = "{}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        var later = new OutboxMessage { EventType = "T2", Payload = "{}", CreatedAt = DateTimeOffset.UtcNow };

        await store.EnqueueAsync(later);
        await store.EnqueueAsync(earlier);

        var batch = await store.FetchPendingAsync(10);
        batch[0].EventType.Should().Be("T1");
        batch[1].EventType.Should().Be("T2");
    }

    [Fact]
    public async Task MarkProcessed_ExcludesFromPending()
    {
        var store = new InMemoryOutboxStore();
        var msg = new OutboxMessage { EventType = "T", Payload = "{}" };
        await store.EnqueueAsync(msg);

        await store.MarkProcessedAsync(msg.Id);

        (await store.FetchPendingAsync(10)).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkFailed_IncrementsAttemptAndRecordsError()
    {
        var store = new InMemoryOutboxStore();
        var msg = new OutboxMessage { EventType = "T", Payload = "{}" };
        await store.EnqueueAsync(msg);

        await store.MarkFailedAsync(msg.Id, "boom");
        await store.MarkFailedAsync(msg.Id, "boom2");

        var snap = store.Snapshot().Single();
        snap.AttemptCount.Should().Be(2);
        snap.LastError.Should().Be("boom2");
        snap.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task FetchPending_RespectsBatchSize()
    {
        var store = new InMemoryOutboxStore();
        for (var i = 0; i < 50; i++)
            await store.EnqueueAsync(new OutboxMessage { EventType = $"T{i}", Payload = "{}" });

        (await store.FetchPendingAsync(10)).Should().HaveCount(10);
    }
}
