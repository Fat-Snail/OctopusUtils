namespace OctopusEx.WebCore.Events.Outbox;

using System.Threading.Channels;

/// <summary>
/// Outbox 唤醒通知器：业务代码 EnqueueAsync 后立即唤醒 dispatcher，
/// 把 polling 延迟从 PollInterval 降到毫秒级。
///
/// 默认实现 <see cref="ChannelOutboxNotifier"/> 用 System.Threading.Channels 的有界 Channel。
/// </summary>
public interface IOutboxNotifier
{
    /// <summary>触发一次唤醒。多次调用合并为单次（Channel 容量 1）。</summary>
    void Notify();

    /// <summary>等待下一次唤醒；若已有未消费的通知则立即返回 true。</summary>
    Task<Boolean> WaitForNotificationAsync(CancellationToken cancellationToken);
}

/// <summary>基于容量 1 的有界 Channel 实现：多次 Notify 合并，避免 dispatcher 跑空轮。</summary>
public sealed class ChannelOutboxNotifier : IOutboxNotifier
{
    private readonly Channel<Byte> _channel = Channel.CreateBounded<Byte>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    public void Notify() => _channel.Writer.TryWrite(0);

    public async Task<Boolean> WaitForNotificationAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _channel.Reader.WaitToReadAsync(cancellationToken)) return false;
            _channel.Reader.TryRead(out _);
            return true;
        }
        catch (OperationCanceledException) { return false; }
    }
}
