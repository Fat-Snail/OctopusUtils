namespace OctopusEx.WebCore.Interceptors.Auditing;

public interface IAuditConfiguration
{
    /// <summary>
    /// 是否启用审计功能
    /// </summary>
    bool Enabled { get; }
        
    /// <summary>
    /// 默认跳过的字段（适用于所有领域）
    /// </summary>
    IReadOnlyCollection<string> GlobalIgnoredProperties { get; }
        
    /// <summary>
    /// 获取特定领域的配置
    /// </summary>
    /// <param name="domainName">领域名称</param>
    /// <returns>领域配置</returns>
    DomainAuditConfiguration GetDomainConfiguration(string domainName);
        
    /// <summary>
    /// 获取用户信息（可从HttpContext、Claims等获取）
    /// </summary>
    /// <returns>用户信息</returns>
    AuditUserInfo GetCurrentUser();
}
    
/// <summary>
/// 领域审计配置
/// </summary>
public class DomainAuditConfiguration
{
    /// <summary>
    /// 是否启用该领域的审计
    /// </summary>
    public bool Enabled { get; set; } = true;
        
    /// <summary>
    /// 该领域需要跳过的字段
    /// </summary>
    public IReadOnlyCollection<string> IgnoredProperties { get; set; } = new List<string>();
        
    /// <summary>
    /// 是否记录所有字段变更（包括未修改的字段）
    /// </summary>
    public bool TrackAllProperties { get; set; } = false;
        
    /// <summary>
    /// 只记录特定字段的变更
    /// </summary>
    public IReadOnlyCollection<string> TrackedProperties { get; set; } = new List<string>();
}
    
/// <summary>
/// 审计用户信息
/// </summary>
public class AuditUserInfo
{
    public string UserId { get; set; } = "system";
    public string UserName { get; set; } = "System User";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
