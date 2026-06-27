namespace OctopusEx.WebCore.Tests.AI;

using Microsoft.Extensions.AI;
using Octopus.AI;

public class ChatHistoryTests
{
    [Fact]
    public void AddUser_And_AddAssistant_AppendInOrder()
    {
        var h = new ChatHistory();
        h.AddSystem("You are helpful.");
        h.AddUser("Hi");
        h.AddAssistant("Hello!");

        var snap = h.Snapshot();
        snap.Should().HaveCount(3);
        snap[0].Role.Should().Be(ChatRole.System);
        snap[1].Role.Should().Be(ChatRole.User);
        snap[2].Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void Trim_KeepsSystemMessages_AndDropsOldestNonSystem()
    {
        var h = new ChatHistory(maxMessages: 4);
        h.AddSystem("sys");
        for (var i = 0; i < 10; i++)
        {
            h.AddUser($"u{i}");
            h.AddAssistant($"a{i}");
        }

        var snap = h.Snapshot();
        // 1 个系统消息 + 4 条最新的非系统消息
        snap.Should().HaveCount(5);
        snap[0].Role.Should().Be(ChatRole.System);
        snap.Last().Text.Should().Be("a9");
    }

    [Fact]
    public void Clear_KeepSystem_RetainsSystemOnly()
    {
        var h = new ChatHistory();
        h.AddSystem("sys");
        h.AddUser("u");
        h.AddAssistant("a");

        h.Clear(keepSystem: true);
        h.Snapshot().Should().ContainSingle(m => m.Role == ChatRole.System);
    }

    [Fact]
    public void Snapshot_ReturnsIndependentCopy()
    {
        var h = new ChatHistory();
        h.AddUser("u");
        var snap1 = h.Snapshot();
        h.AddAssistant("a");

        snap1.Should().HaveCount(1);
        h.Snapshot().Should().HaveCount(2);
    }
}
