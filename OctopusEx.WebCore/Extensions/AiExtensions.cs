namespace OctopusEx.WebCore.Extensions;

using Microsoft.Extensions.AI;
using Octopus.AI;

/// <summary>
/// AI 服务注册扩展。基于 Microsoft.Extensions.AI 标准接口，Provider 由调用方注入。
/// </summary>
public static class AiExtensions
{
    /// <summary>
    /// 注册 OctopusChatService。要求容器中已注册 IChatClient（由具体 Provider 提供，例如：
    /// <code>
    /// builder.Services.AddChatClient(sp =>
    ///     new OllamaChatClient(new Uri("http://localhost:11434"), "llama3"));
    /// builder.Services.AddOctopusAI();
    /// </code>
    /// </summary>
    public static IServiceCollection AddOctopusAI(this IServiceCollection services)
    {
        services.AddSingleton<IOctopusChatService>(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            return new OctopusChatService(chatClient);
        });
        return services;
    }
}
