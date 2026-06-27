namespace OctopusEx.WebCore.Tests.AI;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Octopus.AI;

public class OctopusChatServiceTests
{
    [Fact]
    public async Task AskAsync_SimplePrompt_ReturnsAssistantText()
    {
        var fake = new FakeChatClient(_ => "Hello!");
        var svc = new OctopusChatService(fake);

        var result = await svc.AskAsync("Hi");

        result.Should().Be("Hello!");
        fake.LastMessages.Should().ContainSingle();
        fake.LastMessages[0].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public async Task AskAsync_WithHistory_AppendsResponseToHistory()
    {
        var fake = new FakeChatClient(_ => "OK");
        var svc = new OctopusChatService(fake);
        var history = new ChatHistory();
        history.AddSystem("sys");

        await svc.AskAsync(history, "first");
        await svc.AskAsync(history, "second");

        var snap = history.Snapshot();
        snap.Should().HaveCount(5); // sys + u1 + a1 + u2 + a2
        snap.Last().Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public async Task AskAsyncT_ParsesValidJson()
    {
        var fake = new FakeChatClient(_ => "{\"name\":\"Alice\",\"age\":30}");
        var svc = new OctopusChatService(fake);

        var result = await svc.AskAsync<Person>("anything");

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public async Task AskAsyncT_StripsCodeFence()
    {
        var fake = new FakeChatClient(_ => "```json\n{\"name\":\"Bob\",\"age\":25}\n```");
        var svc = new OctopusChatService(fake);

        var result = await svc.AskAsync<Person>("anything");

        result.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task AskAsyncT_RetriesOnInvalidJson()
    {
        var attempts = 0;
        var fake = new FakeChatClient(_ =>
        {
            attempts++;
            return attempts < 2 ? "not json" : "{\"name\":\"X\",\"age\":1}";
        });
        var svc = new OctopusChatService(fake);

        var result = await svc.AskAsync<Person>("p", maxRetries: 2);

        result.Name.Should().Be("X");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task AskAsyncT_ThrowsAfterAllRetriesFail()
    {
        var fake = new FakeChatClient(_ => "still not json");
        var svc = new OctopusChatService(fake);

        var act = async () => await svc.AskAsync<Person>("p", maxRetries: 1);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StreamAsync_YieldsChunks()
    {
        var fake = new FakeChatClient(_ => "ignored", streamChunks: new[] { "Hel", "lo", "!" });
        var svc = new OctopusChatService(fake);

        var collected = new List<String>();
        await foreach (var chunk in svc.StreamAsync("Hi"))
            collected.Add(chunk);

        collected.Should().Equal("Hel", "lo", "!");
    }

    public record Person(String Name, Int32 Age);

    private sealed class FakeChatClient : IChatClient
    {
        private readonly Func<List<ChatMessage>, String> _responder;
        private readonly String[]? _streamChunks;

        public List<ChatMessage> LastMessages { get; private set; } = new();

        public FakeChatClient(Func<List<ChatMessage>, String> responder, String[]? streamChunks = null)
        {
            _responder = responder;
            _streamChunks = streamChunks;
        }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            var text = _responder(LastMessages);
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            var chunks = _streamChunks ?? new[] { _responder(LastMessages) };
            foreach (var c in chunks)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, c);
                await Task.Yield();
            }
        }

        public Object? GetService(Type serviceType, Object? serviceKey = null) => null;
        public void Dispose() { }
    }
}
