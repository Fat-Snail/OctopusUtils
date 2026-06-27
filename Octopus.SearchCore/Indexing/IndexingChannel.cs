namespace Octopus.SearchCore.Indexing;

using System.Threading.Channels;

/// <summary>
/// 索引操作类型
/// </summary>
public enum IndexOperation { Upsert, Delete }

/// <summary>
/// 索引命令
/// </summary>
public sealed record IndexCommand<TDocument>(IndexOperation Operation, String Id, TDocument? Document);

/// <summary>
/// 基于 System.Threading.Channels 的异步索引队列。
/// 生产者：业务代码 EnqueueUpsert / EnqueueDelete；
/// 消费者：单一后台 worker 批量取出并提交，吞吐量优于逐条 commit。
/// </summary>
public class IndexingChannel<TDocument>
{
    private readonly Channel<IndexCommand<TDocument>> _channel;

    public IndexingChannel(Int32 capacity = 1024)
    {
        _channel = Channel.CreateBounded<IndexCommand<TDocument>>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    /// <summary>排队一个 Upsert 命令。队列满时等待。</summary>
    public ValueTask EnqueueUpsertAsync(String id, TDocument document, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(new IndexCommand<TDocument>(IndexOperation.Upsert, id, document), cancellationToken);

    /// <summary>排队一个 Delete 命令。</summary>
    public ValueTask EnqueueDeleteAsync(String id, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(new IndexCommand<TDocument>(IndexOperation.Delete, id, default), cancellationToken);

    /// <summary>读取通道（消费者使用）。</summary>
    public ChannelReader<IndexCommand<TDocument>> Reader => _channel.Reader;

    /// <summary>关闭通道，停止接收新命令。已排队的命令仍可消费。</summary>
    public void Complete() => _channel.Writer.TryComplete();

    /// <summary>
    /// 从通道读取一批命令（最多 batchSize 条，等待 maxWait 时间）。
    /// 适合消费者循环：拿到一批后批量提交到底层引擎。
    /// </summary>
    public async Task<List<IndexCommand<TDocument>>> ReadBatchAsync(
        Int32 batchSize,
        TimeSpan maxWait,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<IndexCommand<TDocument>>(batchSize);

        // 等待至少一条，否则阻塞直至取消
        if (!await _channel.Reader.WaitToReadAsync(cancellationToken))
            return batch;

        using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        batchCts.CancelAfter(maxWait);

        try
        {
            while (batch.Count < batchSize && _channel.Reader.TryRead(out var cmd))
                batch.Add(cmd);

            // 已取到一些数据；继续等待直到达到 batchSize 或超时
            while (batch.Count < batchSize)
            {
                if (!await _channel.Reader.WaitToReadAsync(batchCts.Token))
                    break;
                while (batch.Count < batchSize && _channel.Reader.TryRead(out var cmd))
                    batch.Add(cmd);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 批量等待超时，返回当前批次
        }

        return batch;
    }
}
