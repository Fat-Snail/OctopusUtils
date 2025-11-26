using Octopus.Tools;

namespace Octopus;

/// <summary>
/// 控制台输出扩展类，提供带颜色的异步控制台写入功能
/// </summary>
public static class ConsoleEx
{
    /// <summary>
    /// 向控制台写入指定颜色的消息（不换行）
    /// </summary>
    /// <param name="message">要写入的消息内容</param>
    /// <param name="consoleColor">控制台文本颜色，默认为绿色</param>
    public static void Write(string message, System.ConsoleColor consoleColor = System.ConsoleColor.Green) =>
        AsyncConsole.Write(ConsoleMessage.Write(message, consoleColor));

    /// <summary>
    /// 向控制台写入指定颜色的消息（换行）
    /// </summary>
    /// <param name="message">要写入的消息内容</param>
    /// <param name="consoleColor">控制台文本颜色，默认为绿色</param>
    public static void WriteLine(string message, System.ConsoleColor consoleColor = System.ConsoleColor.Green) =>
        AsyncConsole.Write(ConsoleMessage.WriteLine(message, consoleColor));

    /// <summary>
    /// 优雅关闭异步控制台，等待所有消息处理完成
    /// </summary>
    /// <param name="timeout">等待超时时间（毫秒），默认为5秒</param>
    public static Task ShutdownAsync(int timeout = 5000) =>
        AsyncConsole.ShutdownAsync(timeout);
}