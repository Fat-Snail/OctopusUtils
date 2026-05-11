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

        var batch = await store.FetchPendingAsync(10, maxAttempts: 5);
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

        (await store.FetchPendingAsync(10, maxAttempts: 5)).Should().BeEmpty();
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
    public async Task FetchPending_FiltersOutMessagesAtOrAboveMaxAttempts()
    {
        var store = new InMemoryOutboxStore();
        var alive = new OutboxMessage { EventType = "T", Payload = "{}", AttemptCount = 2 };
        var dead = new OutboxMessage { EventType = "T", Payload = "{}", AttemptCount = 5 };
        await store.EnqueueAsync(alive);
        await store.EnqueueAsync(dead);

        var batch = await store.FetchPendingAsync(10, maxAttempts: 5);
        batch.Should().ContainSingle().Which.Id.Should().Be(alive.Id);
    }

    [Fact]
    public async Task ChannelOutboxNotifier_NotifyWakesWaitImmediately()
    {
        var notifier = new ChannelOutboxNotifier();
        notifier.Notify();

        var waited = await notifier.WaitForNotificationAsync(CancellationToken.None);
        waited.Should().BeTrue();
    }

    [Fact]
    public async Task ChannelOutboxNotifier_MultipleNotifyCollapseToSingle()
    {
        var notifier = new ChannelOutboxNotifier();
        notifier.Notify();
        notifier.Notify();
        notifier.Notify();

        (await notifier.WaitForNotificationAsync(CancellationToken.None)).Should().BeTrue();

        // 第二次等待应阻塞直至超时（通过短取消验证）
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        (await notifier.WaitForNotificationAsync(cts.Token)).Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryOutboxStore_EnqueueTriggersNotifier()
    {
        var notifier = new ChannelOutboxNotifier();
        var store = new InMemoryOutboxStore(notifier);
        await store.EnqueueAsync(new OutboxMessage { EventType = "T", Payload = "{}" });

        (await notifier.WaitForNotificationAsync(CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task FetchPending_RespectsBatchSize()
    {
        var store = new InMemoryOutboxStore();
        for (var i = 0; i < 50; i++)
            await store.EnqueueAsync(new OutboxMessage { EventType = $"T{i}", Payload = "{}" });

        (await store.FetchPendingAsync(10, maxAttempts: 5)).Should().HaveCount(10);
    }
}
