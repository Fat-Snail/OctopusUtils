namespace Octopus.AI;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

/// <summary>
/// IOctopusChatService 默认实现。包装 IChatClient，添加历史、模板、结构化输出能力。
/// </summary>
public class OctopusChatService : IOctopusChatService
{
    private readonly IChatClient _chatClient;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public OctopusChatService(IChatClient chatClient)
        => _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public async Task<String> AskAsync(String prompt, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
        return ExtractText(response);
    }

    public async Task<String> AskAsync(ChatHistory history, String userPrompt, CancellationToken cancellationToken = default)
    {
        history.AddUser(userPrompt);
        var response = await _chatClient.GetResponseAsync(history.Snapshot(), cancellationToken: cancellationToken);
        var text = ExtractText(response);
        history.AddAssistant(text);
        return text;
    }

    public async Task<T> AskAsync<T>(String prompt, Int32 maxRetries = 2, CancellationToken cancellationToken = default)
    {
        var systemPrompt = $"你必须仅返回符合以下 JSON Schema 的合法 JSON，不要包含任何额外文字、代码块标记或注释。Schema 类型：{typeof(T).Name}";
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, prompt),
        };

        Exception? lastError = null;
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);
                var text = ExtractText(response).Trim();

                // 兼容模型常见的 ```json fenced code block 输出
                if (text.StartsWith("```"))
                    text = StripCodeFence(text);

                var result = JsonSerializer.Deserialize<T>(text, JsonOptions);
                if (result == null)
                    throw new InvalidOperationException("Deserialized to null");
                return result;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastError = ex;
                messages.Add(new ChatMessage(ChatRole.System, $"上一次响应解析失败：{ex.Message}。请严格按 JSON Schema 重新输出。"));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Structured output failed after {maxRetries + 1} attempts: {ex.Message}", ex);
            }
        }

        throw new InvalidOperationException("Unreachable", lastError);
    }

    public async IAsyncEnumerable<String> StreamAsync(String prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, prompt) };
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: cancellationToken))
        {
            var text = ExtractText(update);
            if (!String.IsNullOrEmpty(text)) yield return text;
        }
    }

    public async IAsyncEnumerable<String> StreamAsync(ChatHistory history, String userPrompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        history.AddUser(userPrompt);
        var assembled = new StringBuilder();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(history.Snapshot(), cancellationToken: cancellationToken))
        {
            var text = ExtractText(update);
            if (String.IsNullOrEmpty(text)) continue;
            assembled.Append(text);
            yield return text;
        }

        history.AddAssistant(assembled.ToString());
    }

    private static String ExtractText(ChatResponse response)
        => response.Messages.FirstOrDefault()?.Text ?? "";

    private static String ExtractText(ChatResponseUpdate update) => update.Text ?? "";

    private static String StripCodeFence(String text)
    {
        var firstNewline = text.IndexOf('\n');
        if (firstNewline < 0) return text;
        var inner = text.Substring(firstNewline + 1);
        var lastFence = inner.LastIndexOf("```", StringComparison.Ordinal);
        return lastFence >= 0 ? inner.Substring(0, lastFence).TrimEnd() : inner;
    }
}
