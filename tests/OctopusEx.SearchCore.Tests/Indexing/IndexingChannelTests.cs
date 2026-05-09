namespace OctopusEx.SearchCore.Tests.Indexing;

using Octopus.SearchCore.Indexing;

public class IndexingChannelTests
{
    [Fact]
    public async Task EnqueueAndReadBatch_RoundTrips()
    {
        var channel = new IndexingChannel<String>();
        await channel.EnqueueUpsertAsync("1", "doc1");
        await channel.EnqueueUpsertAsync("2", "doc2");
        await channel.EnqueueDeleteAsync("3");

        var batch = await channel.ReadBatchAsync(batchSize: 10, maxWait: TimeSpan.FromMilliseconds(100));

        batch.Should().HaveCount(3);
        batch[0].Operation.Should().Be(IndexOperation.Upsert);
        batch[0].Id.Should().Be("1");
        batch[2].Operation.Should().Be(IndexOperation.Delete);
    }

    [Fact]
    public async Task ReadBatchAsync_RespectsBatchSize()
    {
        var channel = new IndexingChannel<String>();
        for (var i = 0; i < 10; i++) await channel.EnqueueUpsertAsync(i.ToString(), $"d{i}");

        var batch = await channel.ReadBatchAsync(batchSize: 4, maxWait: TimeSpan.FromMilliseconds(100));
        batch.Should().HaveCount(4);
    }

    [Fact]
    public async Task ReadBatchAsync_TimesOutWithPartialBatch()
    {
        var channel = new IndexingChannel<String>();
        await channel.EnqueueUpsertAsync("1", "a");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var batch = await channel.ReadBatchAsync(batchSize: 100, maxWait: TimeSpan.FromMilliseconds(100));
        sw.Stop();

        batch.Should().HaveCount(1);
        sw.ElapsedMilliseconds.Should().BeInRange(80, 500);
    }

    [Fact]
    public async Task Complete_ClosesChannel()
    {
        var channel = new IndexingChannel<String>();
        await channel.EnqueueUpsertAsync("1", "a");
        channel.Complete();

        var batch = await channel.ReadBatchAsync(10, TimeSpan.FromMilliseconds(50));
        batch.Should().HaveCount(1);

        // 关闭后再读返回空
        var second = await channel.ReadBatchAsync(10, TimeSpan.FromMilliseconds(50));
        second.Should().BeEmpty();
    }
}
