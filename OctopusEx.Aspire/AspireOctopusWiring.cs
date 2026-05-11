namespace OctopusEx.Aspire;

using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// 把常见 Aspire 注入的资源（Redis、配置中心）自动接线到 OctopusEx 抽象。
///
/// 用法：
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.AddOctopusServiceDefaults();   // OTel + ServiceDiscovery + Resilience
/// builder.AddOctopusAspireWiring();      // 自动检测 Redis / 配置中心资源并注册
/// </code>
///
/// 检测规则（按 Aspire 约定的 ConnectionStrings: 节）：
/// - "redis" → 注册 AspireRedisHint，提示用户调用 services.AddStackExchangeRedisCache(connStr)
/// - "octopus-config" → 把该 connection string 解释为 HTTP JSON KV 端点，附加到 IConfiguration
/// </summary>
public static class AspireOctopusWiring
{
    /// <summary>自动接线 Aspire 注入的资源。安全幂等，可重复调用。</summary>
    public static IHostApplicationBuilder AddOctopusAspireWiring(this IHostApplicationBuilder builder)
    {
        var redisConn = builder.Configuration.GetConnectionString("redis");
        if (!String.IsNullOrEmpty(redisConn))
        {
            builder.Services.AddSingleton(new AspireRedisHint(redisConn));
        }

        var configEndpoint = builder.Configuration.GetConnectionString("octopus-config");
        if (!String.IsNullOrEmpty(configEndpoint))
        {
            builder.Configuration.AddRemoteKvSource(configEndpoint);
        }

        return builder;
    }

    /// <summary>
    /// 添加远程 KV 配置源。默认实现：HTTP GET 端点期待 JSON 对象响应，扁平化为 IConfiguration。
    /// 启动时一次性加载；动态刷新需用户实现自定义 IConfigurationProvider。
    ///
    /// 兼容场景：Consul KV（/v1/kv/path?raw）、Vault KV、自建 HTTP JSON 配置中心。
    /// </summary>
    public static IConfigurationBuilder AddRemoteKvSource(this IConfigurationBuilder builder, String endpoint, HttpClient? httpClient = null)
    {
        builder.Add(new RemoteKvConfigSource(endpoint, httpClient));
        return builder;
    }
}

/// <summary>提示型记录：Aspire 已注入 Redis 资源，用户应自行调用 cache 注册扩展。</summary>
public sealed record AspireRedisHint(String ConnectionString);

internal sealed class RemoteKvConfigSource : IConfigurationSource
{
    private readonly String _endpoint;
    private readonly HttpClient? _httpClient;
    public RemoteKvConfigSource(String endpoint, HttpClient? httpClient)
    {
        _endpoint = endpoint;
        _httpClient = httpClient;
    }
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new RemoteKvConfigProvider(_endpoint, _httpClient);
}

/// <summary>
/// HTTP-JSON 远程 KV 配置 Provider。
/// - GET 端点 → 期待 JSON 对象（嵌套支持）
/// - 把嵌套对象用 ":" 分隔展开为扁平 KV 写入 Data
/// - 数组用 ":0", ":1" 索引
/// - 加载失败不抛异常（避免阻塞应用启动），仅记录占位 metadata key 方便诊断
/// </summary>
internal sealed class RemoteKvConfigProvider : ConfigurationProvider
{
    private readonly String _endpoint;
    private readonly HttpClient _httpClient;
    private readonly Boolean _ownsHttpClient;

    public RemoteKvConfigProvider(String endpoint, HttpClient? httpClient)
    {
        _endpoint = endpoint;
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _ownsHttpClient = true;
        }
    }

    public override void Load()
    {
        Data["OctopusEx:Aspire:RemoteKv:Endpoint"] = _endpoint;

        try
        {
            // ConfigurationProvider.Load 是同步签名，无法 await；用 GetAwaiter().GetResult() 阻塞
            var json = _httpClient.GetStringAsync(_endpoint).ConfigureAwait(false).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            FlattenInto(doc.RootElement, prefix: "", Data);
            Data["OctopusEx:Aspire:RemoteKv:Status"] = "loaded";
        }
        catch (Exception ex)
        {
            // 远程配置不可用时退化为本地配置，应用仍可启动
            Data["OctopusEx:Aspire:RemoteKv:Status"] = $"failed: {ex.GetType().Name}";
            Data["OctopusEx:Aspire:RemoteKv:LastError"] = ex.Message;
        }
        finally
        {
            if (_ownsHttpClient) _httpClient.Dispose();
        }
    }

    /// <summary>
    /// 把 JSON 节点扁平化为 IConfiguration 风格 KV 对（用 ":" 分隔嵌套层级）。
    /// 与 Microsoft.Extensions.Configuration.Json 的 JsonConfigurationFileParser 行为一致。
    /// </summary>
    internal static void FlattenInto(JsonElement element, String prefix, IDictionary<String, String?> data)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var nextPrefix = String.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
                    FlattenInto(prop.Value, nextPrefix, data);
                }
                break;
            case JsonValueKind.Array:
                var idx = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenInto(item, $"{prefix}:{idx}", data);
                    idx++;
                }
                break;
            case JsonValueKind.String:
                data[prefix] = element.GetString();
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                data[prefix] = element.ToString();
                break;
            case JsonValueKind.Null:
                data[prefix] = null;
                break;
            // Undefined：跳过
        }
    }
}
