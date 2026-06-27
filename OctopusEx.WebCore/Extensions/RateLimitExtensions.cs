namespace OctopusEx.WebCore.Extensions;

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// 限流配置扩展。封装 ASP.NET Core 原生 RateLimiter，预置三种常用策略与三种分区维度。
/// </summary>
public static class RateLimitExtensions
{
    /// <summary>策略名常量，方便控制器 [EnableRateLimiting] 引用</summary>
    public static class Policies
    {
        /// <summary>固定窗口（每 IP）</summary>
        public const String FixedByIp = "octopus:fixed-by-ip";
        /// <summary>滑动窗口（每用户）</summary>
        public const String SlidingByUser = "octopus:sliding-by-user";
        /// <summary>令牌桶（全局）</summary>
        public const String TokenBucketGlobal = "octopus:token-bucket-global";
    }

    /// <summary>
    /// 注册三个常用限流策略：按 IP 固定窗口、按用户滑动窗口、全局令牌桶。
    /// 用法：
    ///   builder.Services.AddSimpleRateLimit(opts => { opts.IpPermitLimit = 60; });
    ///   app.UseRateLimiter();
    ///   控制器 [EnableRateLimiting(RateLimitExtensions.Policies.FixedByIp)]
    /// </summary>
    public static IServiceCollection AddSimpleRateLimit(
        this IServiceCollection services,
        Action<SimpleRateLimitOptions>? configure = null)
    {
        var opts = new SimpleRateLimitOptions();
        configure?.Invoke(opts);

        services.AddRateLimiter(rateLimiter =>
        {
            rateLimiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            rateLimiter.AddPolicy(Policies.FixedByIp, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.IpPermitLimit,
                        Window = opts.IpWindow,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    }));

            rateLimiter.AddPolicy(Policies.SlidingByUser, ctx =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ctx.User.Identity?.Name
                        ?? ctx.Connection.RemoteIpAddress?.ToString()
                        ?? "anon",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = opts.UserPermitLimit,
                        Window = opts.UserWindow,
                        SegmentsPerWindow = opts.UserSegments,
                        QueueLimit = 0,
                    }));

            rateLimiter.AddPolicy(Policies.TokenBucketGlobal, _ =>
                RateLimitPartition.GetTokenBucketLimiter("global",
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = opts.GlobalTokenLimit,
                        TokensPerPeriod = opts.GlobalTokensPerPeriod,
                        ReplenishmentPeriod = opts.GlobalReplenishmentPeriod,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        return services;
    }
}

/// <summary>简易限流配置</summary>
public class SimpleRateLimitOptions
{
    /// <summary>每 IP 在窗口期内允许的最大请求数</summary>
    public Int32 IpPermitLimit { get; set; } = 100;
    public TimeSpan IpWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>每用户滑动窗口允许的最大请求数</summary>
    public Int32 UserPermitLimit { get; set; } = 200;
    public TimeSpan UserWindow { get; set; } = TimeSpan.FromMinutes(1);
    public Int32 UserSegments { get; set; } = 6;

    /// <summary>全局令牌桶容量</summary>
    public Int32 GlobalTokenLimit { get; set; } = 1000;
    public Int32 GlobalTokensPerPeriod { get; set; } = 100;
    public TimeSpan GlobalReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(1);
}
