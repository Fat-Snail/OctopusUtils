namespace OctopusEx.Segment.Tests.HotReload;

using Octopus.Segment.Abstractions;
using Octopus.Segment.HotReload;

public class DictionaryWatcherTests : IDisposable
{
    private readonly String _dictPath;

    public DictionaryWatcherTests()
    {
        _dictPath = Path.Combine(Path.GetTempPath(), $"octopus-dict-{Guid.NewGuid():N}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_dictPath)) File.Delete(_dictPath);
    }

    [Fact]
    public void ReloadIfExists_OnInitialFile_LoadsAllWords()
    {
        File.WriteAllText(_dictPath, "章鱼组件 1000\n墨鱼工具 500 n\n# this is a comment\n");
        var seg = new JiebaSegmenterAdapter();
        using var watcher = new DictionaryWatcher(seg, _dictPath);

        var added = watcher.ReloadIfExists();

        added.Should().Be(2);
        seg.Cut("章鱼组件很好用").Should().Contain("章鱼组件");
    }

    [Fact]
    public void ReloadIfExists_DedupesAlreadyAddedWords()
    {
        File.WriteAllText(_dictPath, "测试词\n");
        var seg = new JiebaSegmenterAdapter();
        using var watcher = new DictionaryWatcher(seg, _dictPath);

        watcher.ReloadIfExists().Should().Be(1);
        watcher.ReloadIfExists().Should().Be(0); // 第二次相同内容不重复
    }

    [Fact]
    public void Start_OnNonExistentFile_DoesNotThrow()
    {
        var seg = new JiebaSegmenterAdapter();
        using var watcher = new DictionaryWatcher(seg, _dictPath);

        var act = () => watcher.Start();
        act.Should().NotThrow();
    }
}
