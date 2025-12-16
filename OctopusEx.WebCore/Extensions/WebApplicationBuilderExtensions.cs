using Microsoft.AspNetCore.Builder;
using Util.Configs;
using Util.Helpers;

namespace OctopusEx.WebCore.Extensions;

/// <summary>
/// Web应用生成器扩展
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// 转换为Util应用生成器
    /// </summary>
    /// <param name="builder">Web应用生成器</param>
    public static IAppBuilder AsBuild(this WebApplicationBuilder builder)
    {
        builder.CheckNull(nameof(builder));
        return new AppBuilder(builder.Host);
    }
}