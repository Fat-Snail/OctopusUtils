namespace OctopusEx.WebCore.Auth.Jwt;

using System.Security.Claims;

/// <summary>
/// Token 颁发与刷新服务
/// </summary>
public interface ITokenService
{
    /// <summary>生成 access + refresh token 对</summary>
    Task<TokenPair> GenerateAsync(IEnumerable<Claim> claims, CancellationToken cancellationToken = default);

    /// <summary>用 refresh token 换取新 token 对（旧 refresh token 失效）</summary>
    Task<TokenPair> RefreshAsync(String refreshToken, CancellationToken cancellationToken = default);

    /// <summary>吊销 refresh token</summary>
    Task RevokeAsync(String refreshToken, CancellationToken cancellationToken = default);
}

/// <summary>Access + Refresh token 对</summary>
public record TokenPair(String AccessToken, String RefreshToken, DateTimeOffset AccessTokenExpiresAt);
