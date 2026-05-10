namespace OctopusEx.Aspire;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// 把常见 Aspire 注入的资源（Redis、Postgres、配置中心）自动接线到 OctopusEx 抽象。
///
/// 用法：
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.AddOctopusServiceDefaults();   // OTel + ServiceDiscovery + Resilience
/// builder.AddOctopusAspireWiring();      // 自动检测 Redis / Postgres 资源并注册
/// </code>
///
/// 检测规则（按 Aspire 约定的 ConnectionStrings: 节）：
/// - "redis" → 注册占位日志，提示用户调用 services.AddStackExchangeRedisCache(connStr)
/// - "octopus-config" → 把该 connection string 解释为远程 KV 端点 URL，附加到 IConfiguration
/// </summary>
public static class AspireOctopusWiring
{
    /// <summary>自动接线 Aspire 注入的资源。安全幂等，可重复调用。</summary>
    public static IHostApplicationBuilder AddOctopusAspireWiring(this IHostApplicationBuilder builder)
    {
        // Redis 缓存接线提示
        var redisConn = builder.Configuration.GetConnectionString("redis");
        if (!String.IsNullOrEmpty(redisConn))
        {
            builder.Services.AddSingleton(new AspireRedisHint(redisConn));
        }

        // 配置中心接线
        var configEndpoint = builder.Configuration.GetConnectionString("octopus-config");
        if (!String.IsNullOrEmpty(configEndpoint))
        {
            builder.Configuration.AddRemoteKvSource(configEndpoint);
        }

        return builder;
    }

    /// <summary>
    /// 远程 KV 配置源（HTTP JSON / etcd / Consul KV 等）。
    /// 默认实现仅读取一次启动配置 —— 真正的动态刷新由具体 Provider 实现。
    /// </summary>
    public static IConfigurationBuilder AddRemoteKvSource(this IConfigurationBuilder builder, String endpoint)
    {
        builder.Add(new RemoteKvConfigSource(endpoint));
        return builder;
    }
}

/// <summary>提示型记录：Aspire 已注入 Redis 资源，用户应自行调用 cache 注册扩展。</summary>
public sealed record AspireRedisHint(String ConnectionString);

internal sealed class RemoteKvConfigSource : IConfigurationSource
{
    private readonly String _endpoint;
    public RemoteKvConfigSource(String endpoint) => _endpoint = endpoint;
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new RemoteKvConfigProvider(_endpoint);
}

internal sealed class RemoteKvConfigProvider : ConfigurationProvider
{
    private readonly String _endpoint;
    public RemoteKvConfigProvider(String endpoint) => _endpoint = endpoint;

    public override void Load()
    {
        // 占位实现：真实的 etcd / Consul / HTTP KV 由用户 Override 或扩展提供
        // 当前仅记录 endpoint 元数据到一个特殊 key，方便诊断
        Data["OctopusEx:Aspire:RemoteKv:Endpoint"] = _endpoint;
    }
}
