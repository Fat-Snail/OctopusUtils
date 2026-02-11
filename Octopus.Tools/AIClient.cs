namespace Octopus.Tools;

public class AIClient : IDisposable
{
    #region >配置实例，简单粗暴<
    private static String _aiApiDomain = "https://api.openai.com";
    private static String _aiApiKey = String.Empty;
    private static String _aiModel = String.Empty;

    private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static DictionaryCache<String, AIClient> _clientCache = new DictionaryCache<String, AIClient>();
    #endregion

    private HttpClient _httpClient = null;

    public AIClient(String name)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_aiApiDomain);
        if ( !_aiApiKey.IsNullOrEmpty() )
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiApiKey}");
        }
    }

    public async Task<CompletionResponse> CreateChatCompletionAsync(CompletionRequest request)
    {
        var sc = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8,
            "application/json");
        var response = await _httpClient.PostAsync("/v1/chat/completions", sc);
        if ( response.IsSuccessStatusCode )
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CompletionResponse>(content);
        }
        return null;
    }

    public CompletionRequest CreateNormalRequest(Action<CompletionRequest> reqAct)
    {
        if ( _aiModel.IsNullOrEmpty() )
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


    public static AIClient CreateAiChat(String name)
    {
        return _clientCache.GetItem(name, k => new AIClient(k));
    }

    public static void SetClientParams(Action<AISetting> setting)
    {
        var set = new AISetting();
        setting.Invoke(set);

        if ( !set.ApiDomain.IsNullOrEmpty() && set.ApiDomain != _aiApiDomain )
        {
            _aiApiDomain = set.ApiDomain;
        }
        if ( !set.ApiKey.IsNullOrEmpty() && set.ApiKey != _aiApiKey )
        {
            _aiApiKey = set.ApiKey;
        }
        if ( !set.DefaultModel.IsNullOrEmpty() && set.DefaultModel != _aiModel )
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
        public String ApiDomain { get; set; }
        public String ApiKey { get; set; }
        public String DefaultModel { get; set; }
    }
}

public class CompletionRequest
{
    [JsonPropertyName("model")]
    public String Model { get; set; }

    [JsonPropertyName("prompt")]
    public String[] Prompts { get; set; } //= new string[0];
    [JsonPropertyName("messages")]
    public List<CompletionMessage> Messages { get; set; } = new List<CompletionMessage>();

    [JsonPropertyName("suffix")]
    public String Suffix { get; set; } = null;

    [JsonPropertyName("max_tokens")]
    public Int32 MaxTokens { get; set; } = 16;

    [JsonPropertyName("temperature")]
    public Double Temperature { get; set; } = 1;

    [JsonPropertyName("top_p")]
    public Double ProbabilityMass { get; set; } = 1;

    [JsonPropertyName("n")]
    public Int32 CompletionsPerPrompt { get; set; } = 1;

    [JsonPropertyName("stream")]
    public Boolean Stream { get; set; } = false;

    [JsonPropertyName("logprobs")]
    public Int32? LogProbabilities { get; set; } = null;

    [JsonPropertyName("echo")]
    public Boolean Echo { get; set; } = false;

    [JsonPropertyName("stop")]
    public String[] Stop { get; set; } = null;

    [JsonPropertyName("presence_penalty")]
    public Double PresencePenalty { get; set; } = 0;

    [JsonPropertyName("frequency_penalty")]
    public Double FrequencyPenalty { get; set; } = 0;

    [JsonPropertyName("best_of")]
    public Int32 BestOf { get; set; } = 1;

    [JsonPropertyName("logit_bias")]
    public Dictionary<String, Int32> LogitBias { get; set; } =
        new Dictionary<String, Int32>();

    [JsonPropertyName("user")]
    public String User { get; set; } = String.Empty;


}

public class CompletionResponse
{
    [JsonPropertyName("id")]
    public String Id { get; set; }

    [JsonPropertyName("_object")]
    public String Object { get; set; }

    [JsonPropertyName("created")]
    public Int32 Created { get; set; }

    [JsonPropertyName("model")]
    public String Model { get; set; }

    [JsonPropertyName("choices")]
    public CompletionChoice[] Choices { get; set; }

    [JsonPropertyName("usage")]
    public CompletionUsage Usage { get; set; }
}

public class CompletionMessage
{
    [JsonPropertyName("role")]
    public String Role { get; set; }
    [JsonPropertyName("content")]
    public String Content { get; set; }
}

public class CompletionChoice
{
    [JsonPropertyName("index")]
    public Int32 Index { get; set; }

    [JsonPropertyName("message")]
    public CompletionMessage Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public String FinishReason { get; set; }
}

public class CompletionUsage
{
    [JsonPropertyName("prompt_tokens")]
    public Int32 PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public Int32 CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public Int32 TotalTokens { get; set; }
}
