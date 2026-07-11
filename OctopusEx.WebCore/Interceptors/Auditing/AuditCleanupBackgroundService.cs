namespace OctopusEx.WebCore.Interceptors.Auditing;

/// <summary>
/// 审计日志自动清理后台服务。按配置的保留天数定期删除过期记录。
/// 每日凌晨（UTC）执行一次（默认 03:00 UTC）。
/// </summary>
public class AuditCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditRetentionOptions _options;
    private readonly ILogger<AuditCleanupBackgroundService> _logger;

    public AuditCleanupBackgroundService(
        IServiceProvider serviceProvider,
        AuditRetentionOptions options,
        ILogger<AuditCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoCleanup)
        {
            _logger.LogInformation("Audit auto cleanup is disabled");
            return;
        }

        _logger.LogInformation("Audit cleanup started: retention={RetentionDays}d, cleanupTime={Time:hh\\:mm} UTC",
            _options.RetentionDays, _options.CleanupTimeUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 计算距离下一次执行的时间
                var now = DateTimeOffset.UtcNow;
                var nextRun = CalculateNextRun(now);
                var delay = nextRun - now;

                _logger.LogInformation("Next audit cleanup at {NextRun:yyyy-MM-dd HH:mm:ss} UTC (in {Delay:hh\\:mm\\:ss})",
                    nextRun, delay);

                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit cleanup iteration failed");
                // 失败后等 1 小时再重试
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Audit cleanup stopped");
    }

    private DateTimeOffset CalculateNextRun(DateTimeOffset now)
    {
        var todayRun = now.Date + _options.CleanupTimeUtc;
        return todayRun > now ? todayRun : todayRun.AddDays(1);
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IAuditStore>();
        if (store == null)
        {
            _logger.LogWarning("IAuditStore not registered, skipping audit cleanup");
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays);
        var count = await store.DeleteOlderThanAsync(cutoff, ct);
        _logger.LogInformation("Audit cleanup removed {Count} records older than {Cutoff:yyyy-MM-dd}", count, cutoff);
    }
}
