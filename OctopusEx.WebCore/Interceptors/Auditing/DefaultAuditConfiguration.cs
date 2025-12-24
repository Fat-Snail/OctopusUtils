namespace OctopusEx.WebCore.Interceptors.Auditing;

public class DefaultAuditConfiguration : IAuditConfiguration
{
    public bool Enabled { get; set; } = true;

    public IReadOnlyCollection<string> GlobalIgnoredProperties { get; set; } = new List<string>
    {
        "CreatedAt",
        "UpdatedAt",
        "CreatedBy",
        "UpdatedBy",
        "Timestamp",
        "RowVersion"
    };

    private readonly Dictionary<string, DomainAuditConfiguration> _domainConfigurations;

    public DefaultAuditConfiguration()
    {
        _domainConfigurations = new Dictionary<string, DomainAuditConfiguration>
        {
            // 系统管理领域配置
            ["System"] = new DomainAuditConfiguration
            {
                Enabled = true, IgnoredProperties = new List<string> { "PasswordHash", "SecurityStamp" }
            },

            // 产品领域配置
            ["Product"] = new DomainAuditConfiguration
            {
                Enabled = true, IgnoredProperties = new List<string> { "InternalCode", "LastPrice" }
            },

            // 审计日志领域（自身不审计）
            ["Audit"] = new DomainAuditConfiguration { Enabled = false }
        };
    }

    public DomainAuditConfiguration GetDomainConfiguration(string domainName)
    {
        return _domainConfigurations.TryGetValue(domainName, out var config)
            ? config
            : new DomainAuditConfiguration { Enabled = true };
    }

    public AuditUserInfo GetCurrentUser()
    {
        // 这里可以从HttpContext、Claims等获取当前用户信息
        // 暂时返回默认值，实际使用时需要注入HttpContextAccessor等
        return new AuditUserInfo { UserId = "system", UserName = "System User" };
    }

    /// <summary>
    /// 添加或更新领域配置
    /// </summary>
    public void ConfigureDomain(string domainName, DomainAuditConfiguration configuration)
    {
        _domainConfigurations[domainName] = configuration;
    }

    /// <summary>
    /// 禁用特定领域的审计
    /// </summary>
    public void DisableDomain(string domainName)
    {
        if ( _domainConfigurations.ContainsKey(domainName) )
        {
            _domainConfigurations[domainName].Enabled = false;
        }
        else
        {
            _domainConfigurations[domainName] = new DomainAuditConfiguration { Enabled = false };
        }
    }

    /// <summary>
    /// 为特定领域添加忽略字段
    /// </summary>
    public void AddIgnoredProperties(string domainName, params string[] properties)
    {
        if ( !_domainConfigurations.ContainsKey(domainName) )
        {
            _domainConfigurations[domainName] = new DomainAuditConfiguration();
        }

        var config = _domainConfigurations[domainName];
        var ignoredProps = new List<string>(config.IgnoredProperties ?? new List<string>());
        ignoredProps.AddRange(properties);
        config.IgnoredProperties = ignoredProps;
    }
}
