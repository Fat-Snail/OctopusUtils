using System.Collections.Concurrent;

namespace Octopus.Tools;

/// <summary>
/// 异步控制台输出类，提供非阻塞的彩色控制台写入功能
/// </summary>
public static class AsyncConsole
{
    private static readonly ConsoleColor _originalColor = Console.ForegroundColor;
    private static readonly BlockingCollection<ConsoleMessage> _messageQueue = new();
    private static readonly CancellationTokenSource _cancellationTokenSource = new();
    private static readonly Task _processingTask;

    static AsyncConsole()
    {
        _processingTask = Task.Run(ProcessMessagesAsync, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 异步处理控制台消息队列
    /// </summary>
    private static async Task ProcessMessagesAsync()
    {
        try
        {
            foreach (var message in _messageQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                WriteMessage(message);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要处理
        }
        catch (Exception ex)
        {
            // 记录异常但不中断处理
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[AsyncConsole Error] {ex.Message}");
                Console.ForegroundColor = _originalColor;
            }
            catch
            {
                // 如果连错误输出都失败了，静默处理
            }
        }
    }

    /// <summary>
    /// 写入单个消息到控制台
    /// </summary>
    /// <param name="message">控制台消息</param>
    private static void WriteMessage(ConsoleMessage message)
    {
        try
        {
            Console.ForegroundColor = message.Color;
            if (message.NewLine)
            {
                Console.WriteLine(message.Text);
            }
            else
            {
                Console.Write(message.Text);
            }
            Console.ForegroundColor = _originalColor;
        }
        catch
        {
            // 如果输出失败，静默处理以避免异常传播
        }
    }

    /// <summary>
    /// 写入控制台消息到队列
    /// </summary>
    /// <param name="message">要写入的控制台消息</param>
    public static void Write(ConsoleMessage message)
    {
        if (!_cancellationTokenSource.IsCancellationRequested)
        {
            _messageQueue.Add(message);
        }
    }

    /// <summary>
    /// 优雅关闭异步控制台，等待所有消息处理完成
    /// </summary>
    /// <param name="timeout">等待超时时间（毫秒），默认为5秒</param>
    public static async Task ShutdownAsync(Int32 timeout = 5000)
    {
        _messageQueue.CompleteAdding();

        var timeoutTask = Task.Delay(TimeSpan.FromMilliseconds(timeout));
        var completedTask = Task.WhenAny(_processingTask, timeoutTask);

        try
        {
            await completedTask;
            if (completedTask.Result == timeoutTask)
            {
                // 超时强制取消
                _cancellationTokenSource.Cancel();
                try
                {
                    await _processingTask;
                }
                catch
                {
                    // 忽略取消时的异常
                }
            }
        }
        catch (Exception)
        {
            // 其他异常，强制取消
            _cancellationTokenSource.Cancel();
            try
            {
                await _processingTask;
            }
            catch
            {
                // 忽略取消时的异常
            }
        }
        finally
        {
            _cancellationTokenSource.Dispose();
            _messageQueue.Dispose();
        }
    }
}

/// <summary>
/// 控制台消息数据结构，包含消息内容和格式信息
/// </summary>
public readonly struct ConsoleMessage
{
    /// <summary>
    /// 消息文本内容
    /// </summary>
    public String Text { get; }

    /// <summary>
    /// 控制台文本颜色
    /// </summary>
    public ConsoleColor Color { get; }

    /// <summary>
    /// 是否在消息后添加换行符
    /// </summary>
    public Boolean NewLine { get; }

    /// <summary>
    /// 初始化控制台消息
    /// </summary>
    /// <param name="text">消息文本</param>
    /// <param name="color">文本颜色</param>
    /// <param name="newLine">是否换行</param>
    public ConsoleMessage(String text, ConsoleColor color, Boolean newLine = true)
    {
        Text = text ?? String.Empty;
        Color = color;
        NewLine = newLine;
    }

    /// <summary>
    /// 创建带换行的控制台消息
    /// </summary>
    /// <param name="text">消息文本</param>
    /// <param name="color">文本颜色</param>
    /// <returns>控制台消息实例</returns>
    public static ConsoleMessage WriteLine(String text, ConsoleColor color) => new(text, color, true);

    /// <summary>
    /// 创建不带换行的控制台消息
    /// </summary>
    /// <param name="text">消息文本</param>
    /// <param name="color">文本颜色</param>
    /// <returns>控制台消息实例</returns>
    public static ConsoleMessage Write(String text, ConsoleColor color) => new(text, color, false);
}