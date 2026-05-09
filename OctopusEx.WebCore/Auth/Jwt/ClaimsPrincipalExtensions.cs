namespace OctopusEx.WebCore.Auth.Jwt;

using System.Security.Claims;

/// <summary>
/// ClaimsPrincipal 常用 Claim 读取扩展
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>读取 sub 或 NameIdentifier Claim</summary>
    public static String? GetUserId(this ClaimsPrincipal principal)
        => principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>读取用户显示名（Identity.Name 或 unique_name claim）</summary>
    public static String? GetUserName(this ClaimsPrincipal principal)
        => principal.Identity?.Name ?? principal.FindFirst("unique_name")?.Value;

    /// <summary>读取所有角色</summary>
    public static IEnumerable<String> GetRoles(this ClaimsPrincipal principal)
        => principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
}
