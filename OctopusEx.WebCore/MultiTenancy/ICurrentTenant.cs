namespace OctopusEx.WebCore.MultiTenancy;

/// <summary>
/// 当前租户上下文。整个请求生命周期内一致；后台任务场景下可显式 SetTenant。
/// </summary>
public interface ICurrentTenant
{
    /// <summary>当前租户 ID（未识别时为 null）</summary>
    String? TenantId { get; }

    /// <summary>临时切换租户上下文，using 结束后自动还原（适合后台任务/系统操作）</summary>
    IDisposable Use(String? tenantId);
}

/// <summary>基于 AsyncLocal 的默认实现，跨 await 边界保持上下文</summary>
public class CurrentTenant : ICurrentTenant
{
    private readonly AsyncLocal<String?> _current = new();

    public String? TenantId
    {
        get => _current.Value;
        internal set => _current.Value = value;
    }

    public IDisposable Use(String? tenantId)
    {
        var previous = _current.Value;
        _current.Value = tenantId;
        return new TenantScope(this, previous);
    }

    private sealed class TenantScope : IDisposable
    {
        private readonly CurrentTenant _owner;
        private readonly String? _previous;
        public TenantScope(CurrentTenant owner, String? previous) { _owner = owner; _previous = previous; }
        public void Dispose() => _owner._current.Value = _previous;
    }
}
