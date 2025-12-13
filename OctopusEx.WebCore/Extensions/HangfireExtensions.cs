using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using Hangfire;
using Hangfire.MemoryStorage;

namespace OctopusEx.WebCore.Extensions;

public static class HangfireExtensions
{
    /// <summary>
    /// 添加 Hangfire 服务配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureAction">配置动作</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, 
        Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>? configureAction = null)
    {
        if (configureAction != null)
        {
            configureAction(services);
        }
        
        return services;
    }

    /// <summary>
    /// 添加简化的 Hangfire 配置（使用内存存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="workerCount">工作进程数量，默认为 1</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSimpleHangfire(this IServiceCollection services, int workerCount = 1)
    {
        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage());

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount;
        });

        return services;
    }

    /// <summary>
    /// 一次性作业扩展
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <returns>作业标识符</returns>
    public static string AddBackgroundJob(this IServiceProvider serviceProvider, 
        string jobName, Expression<Action> action)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var jobId = backgroundJobClient.Enqueue(action);
            Console.WriteLine($"[HangfireExtensions] 成功添加一次性作业: {jobName}, ID: {jobId}");
            return jobId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HangfireExtensions] 添加一次性作业失败: {ex.Message}");
            Task.Run(action.Compile());
            return $"fallback-{jobName}-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// 延迟作业扩展
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <param name="delay">延迟时间</param>
    /// <returns>作业标识符</returns>
    public static string AddDelayedJob(this IServiceProvider serviceProvider, 
        string jobName, Expression<Action> action, TimeSpan delay)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var jobId = backgroundJobClient.Schedule(action, delay);
            Console.WriteLine($"[HangfireExtensions] 成功添加延迟作业: {jobName}, ID: {jobId}");
            return jobId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HangfireExtensions] 添加延迟作业失败: {ex.Message}");
            Task.Delay(delay).ContinueWith(_ => action.Compile()());
            return $"fallback-{jobName}-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// 循环作业扩展
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobId">作业ID</param>
    /// <param name="action">作业动作</param>
    /// <param name="cronExpression">Cron表达式</param>
    /// <returns>作业标识符</returns>
    public static string AddRecurringJob(this IServiceProvider serviceProvider, 
        string jobId, Expression<Action> action, string cronExpression)
    {
        try
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate(jobId, action, cronExpression);
            Console.WriteLine($"[HangfireExtensions] 成功添加循环作业: {jobId}");
            return jobId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HangfireExtensions] 添加循环作业失败: {ex.Message}");
            var interval = ParseCronToInterval(cronExpression);
            var timer = new Timer(_ => action.Compile()(), null, TimeSpan.Zero, interval);
            return $"fallback-{jobId}";
        }
    }

    /// <summary>
    /// 移除循环作业
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobId">作业ID</param>
    public static void RemoveRecurringJob(this IServiceProvider serviceProvider, string jobId)
    {
        try
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.RemoveIfExists(jobId);
            Console.WriteLine($"[HangfireExtensions] 成功移除循环作业: {jobId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HangfireExtensions] 移除循环作业失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 简单的 Cron 表达式解析器
    /// </summary>
    private static TimeSpan ParseCronToInterval(string cronExpression)
    {
        if (cronExpression == "* * * * *") return TimeSpan.FromMinutes(1); // 每分钟
        if (cronExpression == "0 * * * *") return TimeSpan.FromHours(1);   // 每小时
        if (cronExpression == "0 0 * * *") return TimeSpan.FromDays(1);    // 每天
        if (cronExpression == "0 */6 * * *") return TimeSpan.FromHours(6); // 每6小时
        
        return TimeSpan.FromMinutes(1); // 默认每分钟
    }
}

/// <summary>
/// Hangfire Dashboard 配置选项
/// </summary>
public class HangfireDashboardOptions
{
    public string Path { get; set; } = "/hangfire";
    public object? DashboardOptions { get; set; }
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "password";
}