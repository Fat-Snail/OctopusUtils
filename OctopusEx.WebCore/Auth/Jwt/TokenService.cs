namespace OctopusEx.WebCore.Auth.Jwt;

using System.Security.Claims;
using System.Security.Cryptography;
using Caching;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

/// <summary>
/// 默认 JWT Token 服务。Refresh token 使用 ICacheService 持久化（建议生产环境用 Redis）。
/// </summary>
public class TokenService : ITokenService
{
    private const String RefreshTokenKeyPrefix = "octopus:refresh:";

    private readonly JwtOptions _options;
    private readonly ICacheService _cache;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public TokenService(JwtOptions options, ICacheService cache)
    {
        if (String.IsNullOrEmpty(options.Secret) || Encoding.UTF8.GetByteCount(options.Secret) < 32)
            throw new ArgumentException("JwtOptions.Secret 必须配置且不少于 32 字节", nameof(options));

        _options = options;
        _cache = cache;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = key,
            ClockSkew = options.ClockSkew,
        };
    }

    internal TokenValidationParameters ValidationParameters => _validationParameters;

    public async Task<TokenPair> GenerateAsync(IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(_options.AccessTokenLifetime);

        var claimsList = claims.ToList();
        if (!claimsList.Any(c => c.Type == JwtRegisteredClaimNames.Jti))
            claimsList.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")));

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claimsList,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshToken = GenerateRefreshTokenString();

        await _cache.SetAsync(
            RefreshTokenKeyPrefix + refreshToken,
            new RefreshTokenRecord(
                claimsList.Where(c => c.Type != JwtRegisteredClaimNames.Jti)
                          .Select(c => new ClaimRecord(c.Type, c.Value)).ToList(),
                now.Add(_options.RefreshTokenLifetime)),
            _options.RefreshTokenLifetime,
            cancellationToken);

        return new TokenPair(accessToken, refreshToken, expiresAt);
    }

    public async Task<TokenPair> RefreshAsync(String refreshToken, CancellationToken cancellationToken = default)
    {
        var key = RefreshTokenKeyPrefix + refreshToken;
        var record = await _cache.GetAsync<RefreshTokenRecord>(key, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token 无效或已过期");

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            throw new UnauthorizedAccessException("Refresh token 已过期");
        }

        await _cache.RemoveAsync(key, cancellationToken);

        var claims = record.Claims.Select(c => new Claim(c.Type, c.Value));
        return await GenerateAsync(claims, cancellationToken);
    }

    public Task RevokeAsync(String refreshToken, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(RefreshTokenKeyPrefix + refreshToken, cancellationToken);

    private static String GenerateRefreshTokenString()
    {
        var bytes = new Byte[32];
        RandomNumberGenerator.Fill(bytes);
        return System.Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    internal record RefreshTokenRecord(List<ClaimRecord> Claims, DateTime ExpiresAt);
    internal record ClaimRecord(String Type, String Value);
}
