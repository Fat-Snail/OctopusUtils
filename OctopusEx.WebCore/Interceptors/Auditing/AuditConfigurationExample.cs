namespace OctopusEx.WebCore.Interceptors.Auditing;

using Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class AuditConfigurationExample
{
    /// <summary>
    /// 演示如何配置审计服务
    /// </summary>
    public static void ConfigureAuditingExample(IServiceCollection services)
    {
        services.AddAuditing(config =>
        {
            // 1. 全局启用/禁用审计
            config.Enabled = true;

            // 2. 添加全局忽略字段
            // config.GlobalIgnoredProperties = new List<string> { "CreatedAt", "UpdatedAt", "RowVersion" };

            // 3. 按领域配置审计

            // 产品领域 - 启用审计，但跳过敏感字段
            config.ConfigureDomain("Product",
                new DomainAuditConfiguration
                {
                    Enabled = true,
                    IgnoredProperties = new List<string>
                    {
                        "InternalCode", "LastPrice", "SupplierInfo", "CostPrice"
                    }
                });

            // 用户领域 - 启用审计，严格保护敏感信息
            config.ConfigureDomain("User",
                new DomainAuditConfiguration
                {
                    Enabled = true,
                    IgnoredProperties = new List<string>
                    {
                        "PasswordHash", "SecurityStamp", "Token", "TwoFactorSecret"
                    }
                });

            // 订单领域 - 记录所有字段变更
            config.ConfigureDomain("Order", new DomainAuditConfiguration
            {
                Enabled = true,
                TrackAllProperties = true, // 记录所有字段，即使未修改
                IgnoredProperties = new List<string> { "InternalNotes" }
            });

            // 系统日志领域 - 禁用审计（避免无限递归）
            config.DisableDomain("Audit");
            config.DisableDomain("SystemLog");

            // 配置领域 - 只跟踪特定字段
            config.ConfigureDomain("Configuration", new DomainAuditConfiguration
            {
                Enabled = true,
                TrackedProperties = new List<string> { "Value", "Description" } // 只跟踪这些字段
            });

            // 4. 动态添加忽略字段
            config.AddIgnoredProperties("Product", "TemporaryField1", "TemporaryField2");
        });
    }

    /// <summary>
    /// 在运行时动态修改配置的示例
    /// </summary>
    public static void DynamicConfigurationExample(IServiceProvider serviceProvider)
    {
        var config = serviceProvider.GetService<IAuditConfiguration>() as DefaultAuditConfiguration;
        if ( config != null )
        {
            // 临时禁用审计
            config.Enabled = false;

            // 执行不需要审计的操作...

            // 重新启用审计
            config.Enabled = true;

            // 临时禁用特定领域
            config.DisableDomain("Product");

            // 执行产品相关操作（不会被审计）...

            // 重新启用
            config.ConfigureDomain("Product", new DomainAuditConfiguration { Enabled = true });
        }
    }
}

/// <summary>
/// 基于特性的领域配置示例
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AuditDomainAttribute : Attribute
{
    public string DomainName { get; }
    public bool TrackAllProperties { get; set; }
    public string[] IgnoredProperties { get; set; } = Array.Empty<string>();

    public AuditDomainAttribute(string domainName)
    {
        DomainName = domainName;
    }
}

/// <summary>
/// 使用特性标记的实体示例
/// </summary>
[AuditDomain("Product", IgnoredProperties = new[] { "InternalCode", "CostPrice" })]
public class ProductEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string InternalCode { get; set; } = string.Empty; // 被忽略
    public decimal CostPrice { get; set; } // 被忽略
}

[AuditDomain("User", IgnoredProperties = new[] { "PasswordHash" })]
public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // 被忽略
}
