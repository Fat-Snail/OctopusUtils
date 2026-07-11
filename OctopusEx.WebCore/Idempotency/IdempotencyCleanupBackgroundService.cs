namespace OctopusEx.WebCore.Idempotency;

/// <summary>
/// 幂等键过期清理后台服务。按照配置的清理间隔定期调用 <see cref="IIdempotencyStore.CleanupExpiredAsync"/>。
/// </summary>
public class IdempotencyCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IdempotencyOptions _options;
    private readonly ILogger<IdempotencyCleanupBackgroundService> _logger;

    public IdempotencyCleanupBackgroundService(
        IServiceProvider serviceProvider,
        IdempotencyOptions options,
        ILogger<IdempotencyCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Idempotency cleanup started: interval={Interval:hh\\:mm}", _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 等待下一次清理
                await Task.Delay(_options.CleanupInterval, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Idempotency cleanup iteration failed");
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Idempotency cleanup stopped");
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetService<IIdempotencyStore>();
        if (store == null)
        {
            _logger.LogWarning("IIdempotencyStore not registered, skipping idempotency cleanup");
            return;
        }

        var count = await store.CleanupExpiredAsync(ct);
        if (count > 0)
            _logger.LogInformation("Idempotency cleanup removed {Count} expired records", count);
    }
}
