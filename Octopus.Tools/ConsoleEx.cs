using System;

namespace Octopus.Tools;

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
    public static void Write(String message, ConsoleColor consoleColor = ConsoleColor.Green) =>
        Octopus.Tools.AsyncConsole.Write(Octopus.Tools.ConsoleMessage.Write(message, consoleColor));

    /// <summary>
    /// 向控制台写入指定颜色的消息（换行）
    /// </summary>
    /// <param name="message">要写入的消息内容</param>
    /// <param name="consoleColor">控制台文本颜色，默认为绿色</param>
    public static void WriteLine(String message, ConsoleColor consoleColor = ConsoleColor.Green) =>
        Octopus.Tools.AsyncConsole.Write(Octopus.Tools.ConsoleMessage.WriteLine(message, consoleColor));

    /// <summary>
    /// 输出信息级别日志消息（绿色）
    /// </summary>
    /// <param name="message">日志消息内容</param>
    public static void Info(String message) =>
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}", ConsoleColor.Green);

    /// <summary>
    /// 输出调试级别日志消息（蓝色）
    /// </summary>
    /// <param name="message">日志消息内容</param>
    public static void Debug(String message) =>
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [DEBUG] {message}", ConsoleColor.Blue);

    /// <summary>
    /// 输出警告级别日志消息（黄色）
    /// </summary>
    /// <param name="message">日志消息内容</param>
    public static void Warn(String message) =>
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [WARN] {message}", ConsoleColor.Yellow);

    /// <summary>
    /// 输出错误级别日志消息（红色）
    /// </summary>
    /// <param name="message">日志消息内容</param>
    public static void Error(String message) =>
        WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}", ConsoleColor.Red);

    /// <summary>
    /// 输出异常错误级别日志消息（红色），包含异常详细信息
    /// </summary>
    /// <param name="exception">异常对象</param>
    public static void Error(Exception exception) =>
        Error($"{exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}");

    /// <summary>
    /// 优雅关闭异步控制台，等待所有消息处理完成
    /// </summary>
    /// <param name="timeout">等待超时时间（毫秒），默认为5秒</param>
    public static System.Threading.Tasks.Task ShutdownAsync(Int32 timeout = 5000) =>
        Octopus.Tools.AsyncConsole.ShutdownAsync(timeout);
}