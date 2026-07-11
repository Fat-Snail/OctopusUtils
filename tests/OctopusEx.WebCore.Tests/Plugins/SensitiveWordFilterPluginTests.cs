namespace OctopusEx.WebCore.Tests.Plugins;

using System.Text.Json;
using Moq;
using Octopus.AI;
using OctopusEx.WebCore.Plugins;

/// <summary>
/// SensitiveWordFilterPlugin 重构后的回归测试。
/// 覆盖：ToolGood.Words 词典检测、词库管理、AI 检测三大模块。
/// </summary>
public class SensitiveWordFilterPluginTests
{
    // ==================== A. ToolGood.Words 词典检测 ====================

    [Fact]
    public void DetectSensitiveWords_WithSensitiveContent_ReturnsHasSensitiveWordsTrue()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string input = "这是一段包含暴力的文本";

        // Act
        var json = plugin.DetectSensitiveWords(input);

        // Assert
        var result = JsonSerializer.Deserialize<SensitiveWordDetectionResult>(json);
        result.Should().NotBeNull();
        result!.HasSensitiveWords.Should().BeTrue();
        result.SensitiveWords.Should().Contain("暴力");
        result.DetectionMethod.Should().Be("ToolGood.Words");
        result.OriginalText.Should().Be(input);
    }

    [Fact]
    public void DetectSensitiveWords_WithCleanContent_ReturnsHasSensitiveWordsFalse()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string input = "今天天气真好，适合出去散步";

        // Act
        var json = plugin.DetectSensitiveWords(input);

        // Assert
        var result = JsonSerializer.Deserialize<SensitiveWordDetectionResult>(json);
        result.Should().NotBeNull();
        result!.HasSensitiveWords.Should().BeFalse();
        result.SensitiveWords.Should().BeEmpty();
        result.DetectionMethod.Should().Be("ToolGood.Words");
    }

    [Fact]
    public void DetectSensitiveWords_TooLongInput_ReturnsErrorMessage()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        // 最大允许 10000 字符，构造 10001 字符的超长输入
        var input = new string('a', 10001);

        // Act
        var json = plugin.DetectSensitiveWords(input);

        // Assert
        var result = JsonSerializer.Deserialize<SensitiveWordDetectionResult>(json);
        result.Should().NotBeNull();
        result!.HasSensitiveWords.Should().BeFalse();
        result.SensitiveWords.Should().BeEmpty();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("too long");
        result.ErrorMessage.Should().Contain("10001");
        result.ErrorMessage.Should().Contain("10000");
    }

    [Fact]
    public void DetectSensitiveWords_ReturnsValidJsonFormat()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string input = "普通测试文本";

        // Act
        var json = plugin.DetectSensitiveWords(input);

        // Assert — 验证 JSON 结构与 camelCase 字段名
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.ValueKind.Should().Be(JsonValueKind.Object);

        root.TryGetProperty("originalText", out var originalTextEl).Should().BeTrue();
        originalTextEl.GetString().Should().Be(input);

        root.TryGetProperty("hasSensitiveWords", out var hasSensitiveEl).Should().BeTrue();
        hasSensitiveEl.ValueKind.Should().Be(JsonValueKind.False);

        root.TryGetProperty("sensitiveWords", out var sensitiveWordsEl).Should().BeTrue();
        sensitiveWordsEl.ValueKind.Should().Be(JsonValueKind.Array);

        root.TryGetProperty("detectionMethod", out var detectionMethodEl).Should().BeTrue();
        detectionMethodEl.GetString().Should().Be("ToolGood.Words");

        root.TryGetProperty("errorMessage", out _).Should().BeTrue();
    }

    // ==================== B. 词库管理 ====================

    [Fact]
    public void AddSensitiveWord_NewWord_AddsToWordList()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string newWord = "测试新敏感词";

        // Act
        var message = plugin.AddSensitiveWord(newWord);

        // Assert
        message.Should().Contain("已添加");
        message.Should().Contain(newWord);
        plugin.GetAllSensitiveWords().Should().Contain(newWord);
    }

    [Fact]
    public void AddSensitiveWord_DuplicateWord_ReturnsAlreadyExistsMessage()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string existingWord = "暴力"; // 默认词库中已存在

        // Act
        var message = plugin.AddSensitiveWord(existingWord);

        // Assert
        message.Should().Contain("已存在");
        message.Should().Contain(existingWord);
    }

    [Fact]
    public void RemoveSensitiveWord_ExistingWord_RemovesFromWordList()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string wordToRemove = "暴力"; // 默认词库中已存在
        plugin.GetAllSensitiveWords().Should().Contain(wordToRemove);

        // Act
        var message = plugin.RemoveSensitiveWord(wordToRemove);

        // Assert
        message.Should().Contain("移除");
        message.Should().Contain(wordToRemove);
        plugin.GetAllSensitiveWords().Should().NotContain(wordToRemove);
    }

    [Fact]
    public void RemoveSensitiveWord_NonExistentWord_ReturnsNotFoundMessage()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        const string nonExistentWord = "这个词库里绝对没有";

        // Act
        var message = plugin.RemoveSensitiveWord(nonExistentWord);

        // Assert
        message.Should().Contain("不存在");
        message.Should().Contain(nonExistentWord);
    }

    [Fact]
    public void SetSensitiveWords_FromEnumerable_ReplacesEntireList()
    {
        // Arrange
        var plugin = new SensitiveWordFilterPlugin();
        var newWords = new List<string> { "词A", "词B", "词C" };

        // Act
        plugin.SetSensitiveWords(newWords);

        // Assert
        var allWords = plugin.GetAllSensitiveWords().ToList();
        allWords.Should().HaveCount(3);
        allWords.Should().BeEquivalentTo(newWords);
        allWords.Should().NotContain("暴力"); // 默认词已被替换掉
    }

    // ==================== C. AI 检测 ====================

    [Fact]
    public async Task DetectWithAiAsync_WithoutAiChat_ReturnsErrorResult()
    {
        // Arrange — 使用无参构造，_aiChat 为 null
        var plugin = new SensitiveWordFilterPlugin();
        const string input = "需要检测的文本";

        // Act
        var result = await plugin.DetectWithAiAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.HasSensitiveWords.Should().BeFalse();
        result.Code.Should().Be(0);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().Contain("请先注入");
        result.OriginalText.Should().Be(input);
        result.DetectionMethod.Should().Be("AI");
    }

    [Fact]
    public async Task DetectWithAiAsync_WithAiChat_ReturnsAnalysisResult()
    {
        // Arrange
        var expectedResult = new AITextAnalysisResult
        {
            HasSensitiveWords = true,
            SensitiveWords = new List<string> { "暴力" },
            SensitiveTypes = new List<string> { "暴力" },
            Confidence = 0.95
        };

        var mockChat = new Mock<IOctopusChatService>();
        mockChat
            .Setup(x => x.AskAsync<AITextAnalysisResult>(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var plugin = new SensitiveWordFilterPlugin(mockChat.Object);
        const string input = "这是一段可能包含敏感内容的文本";

        // Act
        var result = await plugin.DetectWithAiAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.HasSensitiveWords.Should().BeTrue();
        result.SensitiveWords.Should().Contain("暴力");
        result.SensitiveTypes.Should().Contain("暴力");
        result.Confidence.Should().Be(0.95);
        result.OriginalText.Should().Be(input);
        result.DetectionMethod.Should().Be("AI");
        result.Code.Should().Be(1);

        // 验证 Mock 被正确调用
        mockChat.Verify(
            x => x.AskAsync<AITextAnalysisResult>(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
