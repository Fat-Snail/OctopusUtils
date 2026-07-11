namespace OctopusEx.WebCore.Plugins;

using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using ToolGood.Words;

/// <summary>
/// 敏感词过滤插件
/// 结合ToolGood.Words和AI识别功能
/// </summary>
public class SensitiveWordFilterPlugin
{
    /// <summary>JSON 序列化选项（camelCase + 缩进），全类共享，避免重复创建</summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private List<string> _sensitiveWords = new List<string>
    {
        "暴力",
        "血腥",
        "色情",
        "低俗",
        "辱骂",
        "歧视",
        "政治",
        "反动",
        "违法",
        "危险",
        "恐怖",
        "仇恨",
        "性爱",
        "性行为",
        "性器官",
        "私密部位",
        "胸部",
        "双峰",
        "双乳",
        "隆起",
        "抚摸",
        "触摸",
        "触碰",
        "露骨",
        "挑逗",
        "引诱",
        "风骚",
        "艳照",
        "裸体",
        "裸露",
        "春光",
        "外泄",
        "黄色",
        "下流",
        "淫秽",
        "不堪入目",
        "色眯眯",
        "轻薄",
        "狎昵",
        "丰满",
        "诱惑",
        "诱惑力",
        "性感",
        "诱人",
        "撩人",
        "酥胸",
        "丰腴",
        "曼妙",
        "婀娜"
    };

    private WordsSearch _wordSearch;
    private readonly Kernel? _kernel;
    private readonly Octopus.AI.IOctopusChatService? _aiChat;

    public SensitiveWordFilterPlugin() : this((Kernel?)null) { }

    public SensitiveWordFilterPlugin(Kernel? kernel = null)
    {
        _kernel = kernel;
        RebuildWordSearch();
    }

    /// <summary>
    /// 推荐：基于 Microsoft.Extensions.AI 的构造方式。比 Kernel 路径更轻量、与 Provider 解耦。
    /// </summary>
    public SensitiveWordFilterPlugin(Octopus.AI.IOctopusChatService aiChat)
    {
        _aiChat = aiChat ?? throw new ArgumentNullException(nameof(aiChat));
        RebuildWordSearch();
    }

    /// <summary>
    /// 使用 IOctopusChatService 做语义敏感词识别。
    /// 优先于 Kernel 路径；返回结构化结果，无需手动解析 JSON。
    /// </summary>
    public async Task<AITextAnalysisResult> DetectWithAiAsync(string input, CancellationToken cancellationToken = default)
    {
        if (_aiChat == null)
        {
            return CreateErrorResult(input, "请先注入 IOctopusChatService（builder.Services.AddOctopusAI()）");
        }

        var prompt = $"请分析以下文本是否包含敏感内容（色情/低俗/暴力/政治/违法等）。\n\n文本：{input}\n\n" +
                     "返回 JSON：{\"originalText\":\"...\",\"hasSensitiveWords\":bool,\"sensitiveWords\":[],\"sensitiveTypes\":[],\"confidence\":0~1,\"detectionMethod\":\"AI\"}";

        try
        {
            var result = await _aiChat.AskAsync<AITextAnalysisResult>(prompt, maxRetries: 2, cancellationToken);
            result.OriginalText = input;
            result.DetectionMethod = "AI";
            result.Code = 1;
            return result;
        }
        catch (Exception ex)
        {
            return CreateErrorResult(input, ex.Message);
        }
    }

    /// <summary>
    /// 从外部设置敏感词库
    /// </summary>
    /// <param name="sensitiveWords">敏感词数组</param>
    public void SetSensitiveWords(IEnumerable<string> sensitiveWords)
    {
        _sensitiveWords.Clear();
        _sensitiveWords.AddRange(sensitiveWords);
        RebuildWordSearch();
    }


    /// <summary>
    /// 获取所有敏感词
    /// </summary>
    /// <returns>敏感词列表</returns>
    public IEnumerable<string> GetAllSensitiveWords()
    {
        return _sensitiveWords.AsReadOnly();
    }

    /// <summary>
    /// 从外部设置完整的敏感词库
    /// </summary>
    /// <param name="sensitiveWordsJson">敏感词JSON数组字符串</param>
    /// <returns>操作结果</returns>
    [KernelFunction, Description("从外部设置完整的敏感词库")]
    public string SetSensitiveWords([Description("敏感词JSON数组字符串")] string sensitiveWordsJson)
    {
        try
        {
            var sensitiveWords = JsonSerializer.Deserialize<List<string>>(sensitiveWordsJson, s_jsonOptions);
            if (sensitiveWords != null)
            {
                _sensitiveWords.Clear();
                _sensitiveWords.AddRange(sensitiveWords);
                RebuildWordSearch();
                return $"敏感词库已更新，共包含 {sensitiveWords.Count} 个敏感词";
            }
            else
            {
                return "错误：无法解析敏感词JSON数据";
            }
        }
        catch (JsonException ex)
        {
            return $"错误：解析敏感词JSON数据失败 - {ex.Message}";
        }
    }

    /// <summary>
    /// 使用外部Kernel实例创建新的插件实例
    /// </summary>
    /// <param name="kernel">Kernel实例</param>
    /// <returns>新的插件实例</returns>
    public static SensitiveWordFilterPlugin CreateWithKernel(Kernel kernel)
    {
        return new SensitiveWordFilterPlugin(kernel);
    }

    /// <summary>
    /// 检测文本中的敏感词（使用ToolGood.Words）
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <returns>JSON格式的检测结果</returns>
    [KernelFunction, Description("使用ToolGood.Words检测文本中的敏感词")]
    public string DetectSensitiveWords([Description("要检测的文本")] string input)
    {
        var result = DetectSensitiveWordsCore(input);
        return JsonSerializer.Serialize(result, s_jsonOptions);
    }

    /// <summary>
    /// AI识别敏感词（使用本地Ollama模型）
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <param name="kernel">Kernel实例</param>
    /// <returns>JSON格式的AI识别结果</returns>
    [KernelFunction, Description("使用AI模型识别文本中的敏感内容")]
    public async Task<string> DetectSensitiveWordsWithAI([Description("要检测的文本")] string input,
        [Description("Kernel实例")] Kernel? kernel = null)
    {
        var result = await DetectWithAiCore(input, kernel);
        return JsonSerializer.Serialize(result, s_jsonOptions);
    }

    /// <summary>
    /// 综合检测文本中的敏感内容（结合ToolGood.Words和AI）
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <returns>综合检测结果</returns>
    [KernelFunction, Description("综合检测文本中的敏感内容（结合ToolGood.Words和AI）")]
    public async Task<string> ComprehensiveDetectSensitiveWords([Description("要检测的文本")] string input)
    {
        // 先使用ToolGood.Words检测（直接调用核心方法，避免 JSON 往返）
        var toolGoodData = DetectSensitiveWordsCore(input);

        // 再使用AI检测（直接调用核心方法，避免 JSON 往返）
        AITextAnalysisResult aiData = _kernel == null
            ? CreateErrorResult(input, "AI service unavailable")
            : await DetectWithAiCore(input, _kernel);

        // 合并结果
        var combinedResult = new CombinedDetectionResult
        {
            OriginalText = input,
            ToolGoodResult = toolGoodData,
            AIResult = aiData,
            FinalHasSensitiveWords = toolGoodData.HasSensitiveWords || aiData.HasSensitiveWords,
            FinalSensitiveWords = toolGoodData.SensitiveWords.Concat(aiData.SensitiveWords).Distinct().ToList(),
            FinalSensitiveTypes = aiData.SensitiveTypes.Distinct().ToList(),
            CombinedConfidence = Math.Max(toolGoodData.HasSensitiveWords ? 1.0 : 0.0, aiData.Confidence),
            DetectionMethod = "Combined"
        };

        return JsonSerializer.Serialize(combinedResult, s_jsonOptions);
    }

    /// <summary>
    /// 添加敏感词到词库
    /// </summary>
    /// <param name="word">要添加的敏感词</param>
    /// <returns>操作结果</returns>
    [KernelFunction, Description("添加敏感词到词库")]
    public string AddSensitiveWord([Description("要添加的敏感词")] string word)
    {
        if (!_sensitiveWords.Contains(word))
        {
            _sensitiveWords.Add(word);
            RebuildWordSearch();
            return $"敏感词 '{word}' 已添加到词库";
        }
        else
        {
            return $"敏感词 '{word}' 已存在于词库中";
        }
    }

    /// <summary>
    /// 从词库中移除敏感词
    /// </summary>
    /// <param name="word">要移除的敏感词</param>
    /// <returns>操作结果</returns>
    [KernelFunction, Description("从词库中移除敏感词")]
    public string RemoveSensitiveWord([Description("要移除的敏感词")] string word)
    {
        bool removed = _sensitiveWords.Remove(word);
        if (removed)
        {
            RebuildWordSearch();
            return $"敏感词 '{word}' 已从词库中移除";
        }
        else
        {
            return $"敏感词 '{word}' 不存在于词库中";
        }
    }

    // ==================== 私有辅助方法 ====================

    /// <summary>
    /// 重建 WordsSearch 索引以应用当前敏感词库
    /// </summary>
    private void RebuildWordSearch()
    {
        _wordSearch = new WordsSearch();
        _wordSearch.SetKeywords(_sensitiveWords);
    }

    /// <summary>
    /// 创建 AI 检测的默认错误结果
    /// </summary>
    /// <param name="input">原始输入文本</param>
    /// <param name="errorMessage">错误信息（可为 null）</param>
    /// <returns>包含错误信息的 AITextAnalysisResult</returns>
    private static AITextAnalysisResult CreateErrorResult(string input, string? errorMessage = null)
    {
        return new AITextAnalysisResult
        {
            OriginalText = input,
            HasSensitiveWords = false,
            SensitiveWords = new List<string>(),
            SensitiveTypes = new List<string>(),
            Confidence = 0.0,
            DetectionMethod = "AI",
            ErrorMessage = errorMessage,
            Code = 0
        };
    }

    /// <summary>
    /// 构建 AI 敏感内容检测提示词
    /// </summary>
    /// <param name="input">待检测文本</param>
    /// <returns>完整的 AI 提示词</returns>
    private static string BuildAiPrompt(string input)
    {
        const string header = "请仔细分析以下文本是否包含任何类型的敏感内容，特别是色情、低俗或成人内容。" +
                              "请严格按照以下JSON格式返回结果，不要添加任何其他内容：\n\n";

        const string jsonFormat = "返回格式：\n" +
                                  "{\n" +
                                  "  \"originalText\": \"原文本\",\n" +
                                  "  \"hasSensitiveWords\": true/false,\n" +
                                  "  \"sensitiveWords\": [\"敏感词1\", \"敏感词2\"],\n" +
                                  "  \"sensitiveTypes\": [\"类型1\", \"类型2\"],\n" +
                                  "  \"confidence\": 0.0-1.0,\n" +
                                  "  \"detectionMethod\": \"AI\"\n" +
                                  "}\n\n";

        const string categories = "特别关注以下类型的敏感内容：\n" +
                                  "- 色情：涉及性行为、性器官、性暗示的身体部位描述\n" +
                                  "- 低俗：粗俗、不雅的表达\n" +
                                  "- 暴力：暴力行为描述\n" +
                                  "- 政治：政治敏感话题\n" +
                                  "- 其他：违法、歧视、恐怖、仇恨等内容\n\n";

        const string rules = "重要：以下情况明确属于敏感内容，必须识别：\n" +
                             "1. 身体部位描述：胸部、乳房、双乳、双峰、臀部、大腿、私密处、下体、阴部等\n" +
                             "2. 触摸接触动作：触摸、触碰、抚摸、亲吻、拥抱、爱抚、揉捏、抓握等\n" +
                             "3. 性暗示描述：诱惑、丰满、隆起、曲线、性感、诱人、撩人、酥胸、丰腴、曼妙、婀娜等\n" +
                             "4. 性行为描述：性交、做爱、爱抚、亲密、缠绵等\n\n" +
                             "严格评估规则：\n" +
                             "- 如果文本包含身体部位词汇+触摸动作词汇的组合，必须标记为敏感内容\n" +
                             "- 如果文本包含性暗示形容词+身体部位的组合，必须标记为敏感内容\n" +
                             "- 如果文本描述对女性身体特征的性暗示描述，必须标记为敏感内容\n\n";

        const string footer = "，必须按上述规则进行判断。\n" +
                              "如果文本符合任何敏感内容定义，confidence值必须设置为0.8以上，sensitiveTypes应包含\"色情\"。\n" +
                              "如果确定是敏感内容但不确定具体分类，至少标记为\"色情\"或\"低俗\"。";

        return header +
               $"文本内容：{input}\n\n" +
               jsonFormat +
               categories +
               rules +
               $"对于当前文本 \"{input}\"{footer}";
    }

    /// <summary>
    /// ToolGood.Words 核心检测逻辑，返回强类型结果。
    /// 公开的 KernelFunction 方法在边界做序列化，综合检测方法直接调用此方法避免 JSON 往返。
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <returns>敏感词检测结果</returns>
    private SensitiveWordDetectionResult DetectSensitiveWordsCore(string input)
    {
        // 检查输入文本长度，防止性能问题
        const int maxInputLength = 10000; // ToolGood.Words可以处理较长文本，但仍需限制
        if (input.Length > maxInputLength)
        {
            return new SensitiveWordDetectionResult
            {
                OriginalText = input,
                HasSensitiveWords = false,
                SensitiveWords = new List<string>(),
                DetectionMethod = "ToolGood.Words",
                ErrorMessage =
                    $"Input text too long ({input.Length} characters). Maximum allowed: {maxInputLength}. Please shorten the input text."
            };
        }

        return new SensitiveWordDetectionResult
        {
            OriginalText = input,
            HasSensitiveWords = _wordSearch.ContainsAny(input),
            SensitiveWords = _wordSearch.FindAll(input).Select(r => r.Keyword).Distinct().ToList(),
            DetectionMethod = "ToolGood.Words"
        };
    }

    /// <summary>
    /// AI 核心检测逻辑（基于 Kernel），返回强类型结果。
    /// 公开的 KernelFunction 方法在边界做序列化，综合检测方法直接调用此方法避免 JSON 往返。
    /// </summary>
    /// <param name="input">输入文本</param>
    /// <param name="kernel">可选的 Kernel 实例，未传入时使用构造时保存的实例</param>
    /// <returns>AI 文本分析结果</returns>
    private async Task<AITextAnalysisResult> DetectWithAiCore(string input, Kernel? kernel)
    {
        // 优先使用传入的Kernel实例，如果未传入则使用内部保存的实例
        Kernel? effectiveKernel = kernel ?? _kernel;

        // 如果没有Kernel实例，则无法进行AI检测
        if (effectiveKernel == null)
        {
            return CreateErrorResult(input, "No kernel provided for AI detection");
        }

        // 检查输入文本长度，防止超出AI的token限制
        const int maxInputLength = 1000; // 可根据需要调整
        if (input.Length > maxInputLength)
        {
            return CreateErrorResult(input,
                $"Input text too long ({input.Length} characters). Maximum allowed: {maxInputLength}. Please shorten the input text.");
        }

        // 创建AI提示，要求返回JSON格式
        var prompt = BuildAiPrompt(input);

        try
        {
            // 使用新版API
            var function = effectiveKernel.CreateFunctionFromPrompt(prompt);
            var result = await effectiveKernel.InvokeAsync(function);
            var resultStr = result.GetValue<string>() ?? result.ToString();

            // 验证返回的是有效的JSON格式
            try
            {
                var parsed = JsonSerializer.Deserialize<AITextAnalysisResult>(resultStr, s_jsonOptions);
                if (parsed == null)
                {
                    // AI 返回 "null" 等极端情况，返回默认错误结果
                    return CreateErrorResult(input);
                }

                // 确保返回原始输入文本，而不是AI可能修改的文本
                parsed.OriginalText = input;
                return parsed;
            }
            catch (JsonException)
            {
                // 如果AI返回的不是有效JSON，构造一个默认结果
                return CreateErrorResult(input);
            }
        }
        catch (Exception ex)
        {
            // 如果AI调用失败，返回默认结果
            return CreateErrorResult(input, ex.Message);
        }
    }
}

/// <summary>
/// 敏感词检测结果
/// </summary>
public class SensitiveWordDetectionResult
{
    [JsonPropertyName("originalText")] public string OriginalText { get; set; } = string.Empty;

    [JsonPropertyName("hasSensitiveWords")]
    public bool HasSensitiveWords { get; set; }

    [JsonPropertyName("sensitiveWords")] public List<string> SensitiveWords { get; set; } = new();

    [JsonPropertyName("detectionMethod")] public string DetectionMethod { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }
}

/// <summary>
/// AI文本分析结果
/// </summary>
public class AITextAnalysisResult
{
    [JsonPropertyName("originalText")] public string OriginalText { get; set; } = string.Empty;

    [JsonPropertyName("hasSensitiveWords")]
    public bool HasSensitiveWords { get; set; }

    [JsonPropertyName("sensitiveWords")] public List<string> SensitiveWords { get; set; } = new();

    [JsonPropertyName("sensitiveTypes")] public List<string> SensitiveTypes { get; set; } = new();

    [JsonPropertyName("confidence")] public double Confidence { get; set; }

    [JsonPropertyName("detectionMethod")] public string DetectionMethod { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }

    [JsonPropertyName("code")] public int Code { get; set; } = 1; // 1表示成功，0表示异常
}

/// <summary>
/// 综合检测结果
/// </summary>
public class CombinedDetectionResult
{
    [JsonPropertyName("originalText")] public string OriginalText { get; set; } = string.Empty;

    [JsonPropertyName("toolGoodResult")] public SensitiveWordDetectionResult ToolGoodResult { get; set; } = new();

    [JsonPropertyName("aiResult")] public AITextAnalysisResult AIResult { get; set; } = new();

    [JsonPropertyName("finalHasSensitiveWords")]
    public bool FinalHasSensitiveWords { get; set; }

    [JsonPropertyName("finalSensitiveWords")]
    public List<string> FinalSensitiveWords { get; set; } = new();

    [JsonPropertyName("finalSensitiveTypes")]
    public List<string> FinalSensitiveTypes { get; set; } = new();

    [JsonPropertyName("combinedConfidence")]
    public double CombinedConfidence { get; set; }

    [JsonPropertyName("detectionMethod")] public string DetectionMethod { get; set; } = string.Empty;
}
