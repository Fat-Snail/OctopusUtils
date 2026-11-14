namespace OctopusEx.Sample.WebApi.Coordination;

using StackExchange.Redis;
using OctopusEx.WebCore.Coordination;

/// <summary>Sample 使用 StackExchange.Redis 实现 WebCore 的 Redis 锁连接抽象。</summary>
public sealed class StackExchangeRedisLockConnection : IRedisDistributedLockConnection
{
    private const String AcquireScript = "return redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2])";
    private const String RenewScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('PEXPIRE', KEYS[1], ARGV[2]) else return 0 end";
    private const String ReleaseScript = "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
    private readonly IDatabase database;

    public StackExchangeRedisLockConnection(IConnectionMultiplexer multiplexer) => database = multiplexer.GetDatabase();

    public async Task<Boolean> TryAcquireAsync(String key, String token, TimeSpan leaseTime, CancellationToken cancellationToken = default)
        => (await database.ScriptEvaluateAsync(AcquireScript, [key], [token, (Int64)leaseTime.TotalMilliseconds]).ConfigureAwait(false)).ToString() == "OK";

    public async Task<Boolean> RenewAsync(String key, String token, TimeSpan leaseTime, CancellationToken cancellationToken = default)
        => (Int64)await database.ScriptEvaluateAsync(RenewScript, [key], [token, (Int64)leaseTime.TotalMilliseconds]).ConfigureAwait(false) == 1;

    public async Task<Boolean> ReleaseAsync(String key, String token, CancellationToken cancellationToken = default)
        => (Int64)await database.ScriptEvaluateAsync(ReleaseScript, [key], [token]).ConfigureAwait(false) == 1;
}
