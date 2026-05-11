namespace OctopusEx.WebCore.Extensions;

using Auth.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// JWT 注册扩展
/// </summary>
public static class JwtExtensions
{
    /// <summary>
    /// 一行开启 JWT Bearer 认证 + 注册 ITokenService。
    /// 调用方需先注册 ICacheService（refresh token 持久化用）。
    /// </summary>
    public static IServiceCollection AddSimpleJwt(
        this IServiceCollection services,
        Action<JwtOptions> configure)
    {
        var options = new JwtOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<ITokenService, TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
            {
                var temp = new TokenService(options, new NoopCacheService());
                jwt.TokenValidationParameters = temp.ValidationParameters;
            });

        services.AddAuthorization();
        return services;
    }

    /// <summary>仅校验 Token 时不需要 cache，提供一个空实现用于初始化校验参数。</summary>
    private sealed class NoopCacheService : Caching.ICacheService
    {
        public Task<T?> GetAsync<T>(String key, CancellationToken ct = default) => Task.FromResult<T?>(default);
        public Task<Caching.CacheResult<T>> TryGetAsync<T>(String key, CancellationToken ct = default) => Task.FromResult(Caching.CacheResult<T>.Miss);
        public Task SetAsync<T>(String key, T value, TimeSpan? ttl = null, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(String key, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Boolean> ExistsAsync(String key, CancellationToken ct = default) => Task.FromResult(false);
        public Task<T?> GetOrAddAsync<T>(String key, Func<CancellationToken, Task<T?>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
            => factory(ct);
    }
}
