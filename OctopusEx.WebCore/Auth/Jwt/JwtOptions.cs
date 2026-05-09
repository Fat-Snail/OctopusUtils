namespace OctopusEx.WebCore.Auth.Jwt;

/// <summary>
/// JWT 配置
/// </summary>
public class JwtOptions
{
    /// <summary>HMAC 签名密钥（建议 32 字节以上）</summary>
    public String Secret { get; set; } = "";

    /// <summary>签发者</summary>
    public String Issuer { get; set; } = "OctopusEx";

    /// <summary>受众</summary>
    public String Audience { get; set; } = "OctopusEx.Api";

    /// <summary>Access Token 有效期</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Refresh Token 有效期（同时也是滑动过期窗口的最大值）</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);

    /// <summary>是否允许刷新时延长 Refresh Token（滑动过期）</summary>
    public Boolean SlidingRefresh { get; set; } = true;

    /// <summary>时钟偏移容忍</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(2);
}
