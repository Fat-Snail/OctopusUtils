namespace Octopus.SearchCore.Indexing;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vector;

/// <summary>
/// 后台批量消费向量索引命令的托管服务。
/// 注册为 IHostedService，启动时即开始消费 IndexingChannel。
/// </summary>
public class VectorIndexingHostedService : BackgroundService
{
    private readonly IndexingChannel<VectorDocument> _channel;
    private readonly IVectorSearchEngine _engine;
    private readonly ILogger<VectorIndexingHostedService> _logger;
    private readonly Int32 _batchSize;
    private readonly TimeSpan _maxWait;

    public VectorIndexingHostedService(
        IndexingChannel<VectorDocument> channel,
        IVectorSearchEngine engine,
        ILogger<VectorIndexingHostedService> logger,
        Int32 batchSize = 100,
        TimeSpan? maxWait = null)
    {
        _channel = channel;
        _engine = engine;
        _logger = logger;
        _batchSize = batchSize;
        _maxWait = maxWait ?? TimeSpan.FromMilliseconds(500);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VectorIndexingHostedService started, batchSize={BatchSize}", _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _channel.ReadBatchAsync(_batchSize, _maxWait, stoppingToken);
                if (batch.Count == 0) continue;

                var upserts = batch.Where(c => c.Operation == IndexOperation.Upsert && c.Document != null)
                    .Select(c => c.Document!).ToList();
                var deletes = batch.Where(c => c.Operation == IndexOperation.Delete).Select(c => c.Id).ToList();

                if (upserts.Count > 0)
                    await _engine.IndexBatchAsync(upserts, stoppingToken);

                foreach (var id in deletes)
                    await _engine.DeleteAsync(id, stoppingToken);

                _logger.LogDebug("Indexed batch: {Upserts} upserts, {Deletes} deletes", upserts.Count, deletes.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VectorIndexingHostedService batch failed, retrying in 1s");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("VectorIndexingHostedService stopped");
    }
}
