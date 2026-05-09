namespace Octopus.AI;

using Microsoft.Extensions.AI;

/// <summary>
/// 对话历史管理（滑动窗口）。
/// 保留 system 消息 + 最近 N 条非系统消息，防止 token 超限。
/// 线程安全：内部用锁保护。
/// </summary>
public class ChatHistory
{
    private readonly Object _lock = new();
    private readonly List<ChatMessage> _messages = new();
    private readonly Int32 _maxMessages;

    /// <param name="maxMessages">非系统消息保留上限，默认 20。系统消息不计入。</param>
    public ChatHistory(Int32 maxMessages = 20) => _maxMessages = maxMessages;

    /// <summary>添加 system 消息（通常用于初始化人设/任务指令）</summary>
    public ChatHistory AddSystem(String content)
    {
        lock (_lock) _messages.Add(new ChatMessage(ChatRole.System, content));
        return this;
    }

    /// <summary>添加用户消息</summary>
    public ChatHistory AddUser(String content)
    {
        lock (_lock)
        {
            _messages.Add(new ChatMessage(ChatRole.User, content));
            Trim();
        }
        return this;
    }

    /// <summary>添加助手消息</summary>
    public ChatHistory AddAssistant(String content)
    {
        lock (_lock)
        {
            _messages.Add(new ChatMessage(ChatRole.Assistant, content));
            Trim();
        }
        return this;
    }

    /// <summary>获取当前消息快照（线程安全副本）</summary>
    public List<ChatMessage> Snapshot()
    {
        lock (_lock) return new List<ChatMessage>(_messages);
    }

    /// <summary>清空历史（保留 system 消息）</summary>
    public void Clear(Boolean keepSystem = true)
    {
        lock (_lock)
        {
            if (keepSystem)
                _messages.RemoveAll(m => m.Role != ChatRole.System);
            else
                _messages.Clear();
        }
    }

    public Int32 Count
    {
        get { lock (_lock) return _messages.Count; }
    }

    private void Trim()
    {
        var nonSystemCount = _messages.Count(m => m.Role != ChatRole.System);
        if (nonSystemCount <= _maxMessages) return;

        var toRemove = nonSystemCount - _maxMessages;
        var i = 0;
        while (i < _messages.Count && toRemove > 0)
        {
            if (_messages[i].Role != ChatRole.System)
            {
                _messages.RemoveAt(i);
                toRemove--;
            }
            else
            {
                i++;
            }
        }
    }
}
