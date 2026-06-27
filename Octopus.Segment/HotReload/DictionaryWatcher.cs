namespace Octopus.Segment.HotReload;

using Abstractions;

/// <summary>
/// 用户词典文件热加载。
/// 监听指定文件的变化（创建 / 修改），自动把新增词条推送到 segmenter。
/// 词典格式：每行 "word [freq] [tag]"，与 JiebaNet 用户词典格式一致。
/// 已添加过的词不会重复 AddWord（按内部 HashSet 去重）。
/// </summary>
public sealed class DictionaryWatcher : IDisposable
{
    private readonly IChineseSegmenter _segmenter;
    private readonly String _filePath;
    private readonly FileSystemWatcher _watcher;
    private readonly HashSet<String> _addedWords = new(StringComparer.Ordinal);
    private readonly Object _lock = new();
    private Boolean _disposed;

    public event Action<Int32>? WordsLoaded;

    public DictionaryWatcher(IChineseSegmenter segmenter, String filePath)
    {
        _segmenter = segmenter ?? throw new ArgumentNullException(nameof(segmenter));
        _filePath = Path.GetFullPath(filePath ?? throw new ArgumentNullException(nameof(filePath)));

        var dir = Path.GetDirectoryName(_filePath)
            ?? throw new ArgumentException("File path must include directory", nameof(filePath));
        var name = Path.GetFileName(_filePath);

        _watcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
    }

    /// <summary>启动监听并立即首次加载。</summary>
    public void Start()
    {
        ReloadIfExists();
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>手动触发重新加载（不等待文件事件）。</summary>
    public Int32 ReloadIfExists()
    {
        if (!File.Exists(_filePath)) return 0;

        Int32 added;
        lock (_lock)
        {
            added = LoadDictionaryFile();
        }
        WordsLoaded?.Invoke(added);
        return added;
    }

    private void OnFileChanged(Object sender, FileSystemEventArgs e)
    {
        // FileSystemWatcher 同一次保存可能触发多次，简单去抖
        try
        {
            Thread.Sleep(50);
            ReloadIfExists();
        }
        catch (Exception)
        {
            // 文件被锁等瞬时异常忽略，下次事件再处理
        }
    }

    private Int32 LoadDictionaryFile()
    {
        var added = 0;
        // 用 FileShare.ReadWrite 避免与外部写入者冲突
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);

        String? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (String.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            var word = parts[0];
            if (_addedWords.Contains(word)) continue;

            var freq = parts.Length > 1 && Int32.TryParse(parts[1], out var f) ? f : 0;
            var tag = parts.Length > 2 ? parts[2] : null;

            _segmenter.AddWord(word, freq, tag);
            _addedWords.Add(word);
            added++;
        }
        return added;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Dispose();
    }
}
