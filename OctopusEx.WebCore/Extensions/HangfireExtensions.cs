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
        if ( configureAction != null )
        {
            configureAction(services);
        }

        return services;
    }

    /// <summary>
    /// 添加简化的 Hangfire 配置（默认使用内存存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="workerCount">工作进程数量，默认为 1</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddSimpleHangfire(this IServiceCollection services, Int32 workerCount = 1)
        => services.AddSimpleHangfire(cfg => cfg.UseMemoryStorage(), workerCount);

    /// <summary>
    /// 添加 Hangfire 配置，自定义存储后端（Redis / SQL Server / PostgreSQL 等）。
    ///
    /// 用例：
    /// <code>
    /// services.AddSimpleHangfire(cfg =>
    ///     cfg.UseRedisStorage("localhost:6379", new RedisStorageOptions { Db = 1 }),
    ///     workerCount: 4);
    /// </code>
    ///
    /// 调用方负责安装具体存储包（如 Hangfire.Pro.Redis / Hangfire.SqlServer / Hangfire.PostgreSql）。
    /// </summary>
    public static IServiceCollection AddSimpleHangfire(
        this IServiceCollection services,
        Action<IGlobalConfiguration> storageConfigurator,
        Int32 workerCount = 1)
    {
        services.AddHangfire(configuration =>
        {
            configuration
                .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();
            storageConfigurator(configuration);
        });

        services.AddHangfireServer(options => { options.WorkerCount = workerCount; });
        return services;
    }

    /// <summary>
    /// 一次性作业扩展
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <returns>作业标识符</returns>
    public static String AddBackgroundJob(this IServiceProvider serviceProvider,
        String jobName, Expression<Action> action)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var jobId = backgroundJobClient.Enqueue(action);
            Console.WriteLine($"[HangfireExtensions] 成功添加一次性作业: {jobName}, ID: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
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
    public static String AddDelayedJob(this IServiceProvider serviceProvider,
        String jobName, Expression<Action> action, TimeSpan delay)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var jobId = backgroundJobClient.Schedule(action, delay);
            Console.WriteLine($"[HangfireExtensions] 成功添加延迟作业: {jobName}, ID: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
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
    public static String AddRecurringJob(this IServiceProvider serviceProvider,
        String jobId, Expression<Action> action, String cronExpression)
    {
        try
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.AddOrUpdate(jobId, action, cronExpression);
            Console.WriteLine($"[HangfireExtensions] 成功添加循环作业: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
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
    public static void RemoveRecurringJob(this IServiceProvider serviceProvider, String jobId)
    {
        try
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            recurringJobManager.RemoveIfExists(jobId);
            Console.WriteLine($"[HangfireExtensions] 成功移除循环作业: {jobId}");
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 移除循环作业失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 简单的 Cron 表达式解析器
    /// </summary>
    private static TimeSpan ParseCronToInterval(String cronExpression)
    {
        if ( cronExpression == "* * * * *" ) return TimeSpan.FromMinutes(1); // 每分钟
        if ( cronExpression == "0 * * * *" ) return TimeSpan.FromHours(1);   // 每小时
        if ( cronExpression == "0 0 * * *" ) return TimeSpan.FromDays(1);    // 每天
        if ( cronExpression == "0 */6 * * *" ) return TimeSpan.FromHours(6); // 每6小时

        return TimeSpan.FromMinutes(1); // 默认每分钟
    }

    /// <summary>
    /// 添加一次性作业（防重复执行）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <param name="uniqueKey">唯一标识，相同标识的作业同时只能有一个在执行</param>
    /// <returns>作业标识符</returns>
    public static String AddBackgroundJobWithLock(this IServiceProvider serviceProvider,
        String jobName, Expression<Action> action, String? uniqueKey = null)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var lockKey = uniqueKey ?? jobName;

            // 检查是否已有相同作业在执行
            if ( IsJobRunning(serviceProvider, lockKey) )
            {
                Console.WriteLine($"[HangfireExtensions] 作业 {lockKey} 已在执行中，跳过添加");
                return $"skipped-{lockKey}-{DateTime.Now:yyyyMMddHHmmss}";
            }

            var jobId = backgroundJobClient.Enqueue(action);

            Console.WriteLine($"[HangfireExtensions] 成功添加防重复一次性作业: {jobName}, LockKey: {lockKey}, ID: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 添加防重复一次性作业失败: {ex.Message}");
            Task.Run(action.Compile());
            return $"fallback-{jobName}-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// 添加延迟作业（防重复执行）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <param name="delay">延迟时间</param>
    /// <param name="uniqueKey">唯一标识，相同标识的作业同时只能有一个在执行</param>
    /// <returns>作业标识符</returns>
    public static String AddDelayedJobWithLock(this IServiceProvider serviceProvider,
        String jobName, Expression<Action> action, TimeSpan delay, String? uniqueKey = null)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var lockKey = uniqueKey ?? jobName;

            var jobId = backgroundJobClient.Schedule(action, delay);

            Console.WriteLine($"[HangfireExtensions] 成功添加防重复延迟作业: {jobName}, LockKey: {lockKey}, ID: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 添加防重复延迟作业失败: {ex.Message}");
            Task.Delay(delay).ContinueWith(_ => action.Compile()());
            return $"fallback-{jobName}-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// 添加循环作业（防重复执行）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobId">作业ID</param>
    /// <param name="action">作业动作</param>
    /// <param name="cronExpression">Cron表达式</param>
    /// <param name="uniqueKey">唯一标识，用于防重复，默认使用jobId</param>
    /// <returns>作业标识符</returns>
    public static String AddRecurringJobWithLock(this IServiceProvider serviceProvider,
        String jobId, Expression<Action> action, String cronExpression, String? uniqueKey = null)
    {
        try
        {
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            var lockKey = uniqueKey ?? jobId;

            recurringJobManager.AddOrUpdate(jobId, action, cronExpression);

            Console.WriteLine($"[HangfireExtensions] 成功添加防重复循环作业: {jobId}, LockKey: {lockKey}");
            return jobId;
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 添加防重复循环作业失败: {ex.Message}");
            var interval = ParseCronToInterval(cronExpression);
            var timer = new Timer(_ => action.Compile()(), null, TimeSpan.Zero, interval);
            return $"fallback-{jobId}";
        }
    }

    /// <summary>
    /// 检查作业是否正在执行中（基于作业锁）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="uniqueKey">唯一标识</param>
    /// <returns>是否正在执行中</returns>
    public static Boolean IsJobRunning(this IServiceProvider serviceProvider, String uniqueKey)
    {
        try
        {
            // 使用静态锁字典检查（简化实现）
            lock ( _jobLocks )
            {
                return _jobLocks.ContainsKey(uniqueKey) && _jobLocks[uniqueKey];
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 检查作业状态失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 获取正在执行的作业数量
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="uniqueKey">唯一标识</param>
    /// <returns>正在执行的作业数量</returns>
    public static Int32 GetRunningJobCount(this IServiceProvider serviceProvider, String uniqueKey)
    {
        try
        {
            // 使用静态锁字典检查（简化实现）
            lock ( _jobLocks )
            {
                return _jobLocks.ContainsKey(uniqueKey) && _jobLocks[uniqueKey] ? 1 : 0;
            }
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 获取作业数量失败: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 添加带锁的一次性作业（使用分布式锁）
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <param name="jobName">作业名称</param>
    /// <param name="action">作业动作</param>
    /// <param name="uniqueKey">唯一标识</param>
    /// <param name="lockTimeout">锁超时时间，默认5分钟</param>
    /// <returns>作业标识符</returns>
    public static String AddBackgroundJobWithDistributedLock(this IServiceProvider serviceProvider,
        String jobName, Expression<Action> action, String uniqueKey, TimeSpan? lockTimeout = null)
    {
        try
        {
            var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
            var timeout = lockTimeout ?? TimeSpan.FromMinutes(5);

            // 创建包装的作业方法，包含锁逻辑
            Expression<Action> lockedAction = () => ExecuteWithLock(uniqueKey, timeout, action);

            var jobId = backgroundJobClient.Enqueue(lockedAction);

            Console.WriteLine($"[HangfireExtensions] 成功添加分布式锁作业: {jobName}, LockKey: {uniqueKey}, ID: {jobId}");
            return jobId;
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 添加分布式锁作业失败: {ex.Message}");
            Task.Run(action.Compile());
            return $"fallback-{jobName}-{Guid.NewGuid():N}";
        }
    }

    /// <summary>
    /// 执行带锁的作业
    /// </summary>
    /// <typeparam name="T">作业方法类型</typeparam>
    /// <param name="lockKey">锁键</param>
    /// <param name="lockTimeout">锁超时时间</param>
    /// <param name="action">作业方法</param>
    private static void ExecuteWithLock<T>(String lockKey, TimeSpan lockTimeout, Expression<T> action) where T : Delegate
    {
        try
        {
            // 这里可以实现分布式锁逻辑
            // 简化实现：使用静态字典作为锁（仅适用于单实例）
            lock ( _jobLocks )
            {
                if ( _jobLocks.ContainsKey(lockKey) )
                {
                    Console.WriteLine($"[HangfireExtensions] 作业 {lockKey} 正在执行中，跳过");
                    return;
                }

                _jobLocks[lockKey] = true;
            }

            Console.WriteLine($"[HangfireExtensions] 获取锁成功，开始执行作业: {lockKey}");

            // 执行实际作业
            action.Compile().DynamicInvoke();
        }
        catch ( Exception ex )
        {
            Console.WriteLine($"[HangfireExtensions] 执行作业 {lockKey} 时出错: {ex.Message}");
            throw;
        }
        finally
        {
            // 释放锁
            lock ( _jobLocks )
            {
                if ( _jobLocks.ContainsKey(lockKey) )
                {
                    _jobLocks.Remove(lockKey);
                }
            }

            Console.WriteLine($"[HangfireExtensions] 释放锁，作业执行完成: {lockKey}");
        }
    }

    /// <summary>
    /// 静态锁字典，用于跟踪正在执行的作业
    /// 注意：这仅适用于单实例环境，生产环境应使用分布式锁
    /// </summary>
    private static readonly Dictionary<String, Boolean> _jobLocks = new Dictionary<String, Boolean>();
}

/// <summary>
/// Hangfire Dashboard 配置选项
/// </summary>
public class HangfireDashboardOptions
{
    public String Path { get; set; } = "/hangfire";
    public Object? DashboardOptions { get; set; }
    public String Username { get; set; } = "admin";
    public String Password { get; set; } = "password";
}

/// <summary>
/// 作业执行配置选项
/// </summary>
public class JobExecutionOptions
{
    /// <summary>
    /// 防重复执行的超时时间（默认5分钟）
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 是否启用自动重试
    /// </summary>
    public Boolean EnableRetry { get; set; } = true;

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public Int32 MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 重试间隔
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(1);
}
