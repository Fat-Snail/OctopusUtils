namespace OctopusEx.WebCore.Tests.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using OctopusEx.WebCore.Auth.Jwt;
using OctopusEx.WebCore.Caching;

public class TokenServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly MemoryCacheService _cache;
    private readonly JwtOptions _options;
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        _cache = new MemoryCacheService(_memoryCache, new CacheOptions { TtlJitterPercent = 0 });
        _options = new JwtOptions
        {
            Secret = "this-is-a-very-long-secret-for-testing-only-1234567890",
            Issuer = "test",
            Audience = "test-aud",
            AccessTokenLifetime = TimeSpan.FromMinutes(5),
            RefreshTokenLifetime = TimeSpan.FromHours(1),
        };
        _service = new TokenService(_options, _cache);
    }

    public void Dispose() { _cache.Dispose(); _memoryCache.Dispose(); }

    [Fact]
    public void Constructor_RejectsShortSecret()
    {
        var act = () => new TokenService(new JwtOptions { Secret = "short" }, _cache);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task GenerateAsync_ProducesValidJwt()
    {
        var claims = new[] { new Claim("sub", "user-1"), new Claim("name", "alice") };
        var pair = await _service.GenerateAsync(claims);

        pair.AccessToken.Should().NotBeNullOrEmpty();
        pair.RefreshToken.Should().NotBeNullOrEmpty();
        pair.AccessTokenExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(pair.AccessToken);
        token.Issuer.Should().Be("test");
        token.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "user-1");
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_IssuesNewPair_AndInvalidatesOld()
    {
        var claims = new[] { new Claim("sub", "user-1") };
        var first = await _service.GenerateAsync(claims);

        var second = await _service.RefreshAsync(first.RefreshToken);

        second.AccessToken.Should().NotBe(first.AccessToken);
        second.RefreshToken.Should().NotBe(first.RefreshToken);

        // 旧 refresh token 必须失效
        var act = async () => await _service.RefreshAsync(first.RefreshToken);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_Throws()
    {
        var act = async () => await _service.RefreshAsync("does-not-exist");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RevokeAsync_InvalidatesRefreshToken()
    {
        var pair = await _service.GenerateAsync(new[] { new Claim("sub", "u") });
        await _service.RevokeAsync(pair.RefreshToken);

        var act = async () => await _service.RefreshAsync(pair.RefreshToken);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public void ClaimsPrincipalExtensions_GetUserId_FromSub()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "u-1") }));
        principal.GetUserId().Should().Be("u-1");
    }

    [Fact]
    public void ClaimsPrincipalExtensions_GetRoles_ReturnsAll()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "editor"),
        }));
        principal.GetRoles().Should().BeEquivalentTo("admin", "editor");
    }
}
