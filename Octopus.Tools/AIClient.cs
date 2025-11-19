using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NewLife;

namespace Octopus.Tools;

public class AIClient : IDisposable
{
    #region >配置实例，简单粗暴<
    private static string _aiApiDomain = "https://api.openai.com";
    private static string _aiApiKey = string.Empty;
    private static string _aiModel = string.Empty;

    private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static DictionaryCache<string, AIClient> _clientCache = new DictionaryCache<string, AIClient>();
    #endregion

    private HttpClient _httpClient = null;

    public AIClient(string name)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_aiApiDomain);
        if (!_aiApiKey.IsNullOrEmpty())
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiApiKey}");
        }
    }

    public async Task<CompletionResponse> CreateChatCompletionAsync(CompletionRequest request)
    {
        var sc = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8,
            "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", sc);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CompletionResponse>(content);
        }
        return null;
    }

    public CompletionRequest CreateNormalRequest(Action<CompletionRequest> reqAct)
    {
        if (_aiModel.IsNullOrEmpty())
        {
            throw new Exception("必须指定默认的model");
        }

        var request = new CompletionRequest()
        {
            FrequencyPenalty = 0,
            Model = _aiModel,
            Temperature = 0.5,
            PresencePenalty = 0,
            Stream = false,
            Messages = new List<CompletionMessage>()
        };

        reqAct.Invoke(request);

        return request;
    }


    public static AIClient CreateAiChat(string name)
    {
        return _clientCache.GetItem(name, k => new AIClient(k));
    }

    public static void SetClientParams(Action<AISetting> setting)
    {
        var set = new AISetting();
        setting.Invoke(set);

        if (!set.ApiDomain.IsNullOrEmpty() && set.ApiDomain != _aiApiDomain)
        {
            _aiApiDomain = set.ApiDomain;
        }
        if (!set.ApiKey.IsNullOrEmpty() && set.ApiKey != _aiApiKey)
        {
            _aiApiKey = set.ApiKey;
        }
        if (!set.DefaultModel.IsNullOrEmpty() && set.DefaultModel != _aiModel)
        {
            _aiModel = set.DefaultModel;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    public class AISetting
    {
        public string ApiDomain { get; set; }
        public string ApiKey { get; set; }
        public string DefaultModel { get; set; }
    }
}

public class CompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("prompt")]
    public string[] Prompts { get; set; } //= new string[0];
    [JsonPropertyName("messages")]
    public List<CompletionMessage> Messages { get; set; } = new List<CompletionMessage>();

    [JsonPropertyName("suffix")]
    public string Suffix { get; set; } = null;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 16;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1;

    [JsonPropertyName("top_p")]
    public double ProbabilityMass { get; set; } = 1;

    [JsonPropertyName("n")]
    public int CompletionsPerPrompt { get; set; } = 1;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("logprobs")]
    public int? LogProbabilities { get; set; } = null;

    [JsonPropertyName("echo")]
    public bool Echo { get; set; } = false;

    [JsonPropertyName("stop")]
    public string[] Stop { get; set; } = null;

    [JsonPropertyName("presence_penalty")]
    public double PresencePenalty { get; set; } = 0;

    [JsonPropertyName("frequency_penalty")]
    public double FrequencyPenalty { get; set; } = 0;

    [JsonPropertyName("best_of")]
    public int BestOf { get; set; } = 1;

    [JsonPropertyName("logit_bias")]
    public Dictionary<string, int> LogitBias { get; set; } =
        new Dictionary<string, int>();

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;


}

public class CompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("_object")]
    public string Object { get; set; }

    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("choices")]
    public CompletionChoice[] Choices { get; set; }

    [JsonPropertyName("usage")]
    public CompletionUsage Usage { get; set; }
}

public class CompletionMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }
    [JsonPropertyName("content")]
    public string Content { get; set; }
}

public class CompletionChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public CompletionMessage Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class CompletionUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}