using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Octopus.Tools.UnitTest;
/// <summary>
/// 迷你性能分析器 - 类似MiniProfiler的功能
/// </summary>
public class MiniProfiler : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly List<MiniStep> _steps;
    private readonly string _name;
    private bool _disposed;

    public string Name => _name;
    public TimeSpan Duration => _stopwatch.Elapsed;
    public IReadOnlyList<MiniStep> Steps => _steps.AsReadOnly();

    private MiniProfiler(string name)
    {
        _name = name;
        _stopwatch = Stopwatch.StartNew();
        _steps = new List<MiniStep>();
    }

    /// <summary>
    /// 开始一个新的性能分析会话
    /// </summary>
    public static MiniProfiler StartNew(string name = "Root")
    {
        return new MiniProfiler(name);
    }

    /// <summary>
    /// 创建一个性能分析步骤
    /// </summary>
    public MiniStep Step(string stepName)
    {
        var step = new MiniStep(stepName);
        _steps.Add(step);
        return step;
    }

    /// <summary>
    /// 获取详细的性能报告
    /// </summary>
    public MiniReport GetReport()
    {
        return new MiniReport(this);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// 迷你性能分析步骤
/// </summary>
public class MiniStep : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private bool _disposed;

    public string Name { get; }
    public TimeSpan Duration => _stopwatch.Elapsed;

    public MiniStep(string name)
    {
        Name = name;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stopwatch.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// 迷你性能报告
/// </summary>
public class MiniReport
{
    public string Name { get; }
    public TimeSpan TotalDuration { get; }
    public IReadOnlyList<MiniStep> Steps { get; }
    public string HierarchyTree { get; }

    public MiniReport(MiniProfiler profiler)
    {
        Name = profiler.Name;
        TotalDuration = profiler.Duration;
        Steps = profiler.Steps;
        HierarchyTree = BuildHierarchyTree(profiler);
    }

    private string BuildHierarchyTree(MiniProfiler profiler)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"├─ {profiler.Name}: {profiler.Duration.TotalMilliseconds:F2}ms");

        foreach (var step in profiler.Steps)
        {
            sb.AppendLine($"│  ├─ {step.Name}: {step.Duration.TotalMilliseconds:F2}ms");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"性能分析报告: {Name}");
        sb.AppendLine($"总耗时: {TotalDuration.TotalMilliseconds:F2}ms");
        sb.AppendLine("调用层级:");
        sb.AppendLine(HierarchyTree);
        return sb.ToString();
    }
}

/// <summary>
/// 性能分析工具类
/// </summary>
public static class MiniProfilerHelper
{
    /// <summary>
    /// 分析指定方法的执行性能
    /// </summary>
    public static T Profile<T>(string operationName, Func<T> operation)
    {
        using var profiler = MiniProfiler.StartNew(operationName);
        return operation();
    }

    /// <summary>
    /// 分析指定方法的执行性能（无返回值）
    /// </summary>
    public static void Profile(string operationName, Action operation)
    {
        using var profiler = MiniProfiler.StartNew(operationName);
        operation();
    }
}