namespace Octopus.AI;

/// <summary>
/// 基于 Microsoft.Extensions.AI 的高阶 Chat 服务。
/// 在 IChatClient 之上提供：对话历史、Prompt 模板、流式输出、结构化反序列化。
/// </summary>
public interface IOctopusChatService
{
    /// <summary>单轮对话，无历史。</summary>
    Task<String> AskAsync(String prompt, CancellationToken cancellationToken = default);

    /// <summary>带历史的多轮对话。返回 assistant 回复并自动追加到 history。</summary>
    Task<String> AskAsync(ChatHistory history, String userPrompt, CancellationToken cancellationToken = default);

    /// <summary>结构化输出。响应自动解析为 T，失败时按配置重试。</summary>
    Task<T> AskAsync<T>(String prompt, Int32 maxRetries = 2, CancellationToken cancellationToken = default);

    /// <summary>流式输出，逐 token 推送。</summary>
    IAsyncEnumerable<String> StreamAsync(String prompt, CancellationToken cancellationToken = default);

    /// <summary>带历史的流式输出。完整 assistant 回复在 enumerator 结束后追加到 history。</summary>
    IAsyncEnumerable<String> StreamAsync(ChatHistory history, String userPrompt, CancellationToken cancellationToken = default);
}
