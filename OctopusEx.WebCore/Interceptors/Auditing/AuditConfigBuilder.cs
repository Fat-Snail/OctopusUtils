using System.Linq.Expressions;

namespace OctopusEx.WebCore.Interceptors.Auditing;

/// <summary>
/// 支持Lambda表达式配置构建器
/// </summary>
public static class AuditConfigBuilder
{
    /// <summary>
    /// 为特定实体类型创建配置
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="configureAction">配置动作</param>
    /// <returns>包含领域名称和配置的元组</returns>
    public static (string DomainName, DomainAuditConfiguration Config) For<TEntity>(
        Action<LambdaConfig<TEntity>> configureAction)
    {
        var config = new LambdaConfig<TEntity>();
        configureAction(config);

        // 根据实体类型自动推断领域名称
        var domainName = GetDomainNameFromEntity<TEntity>();
        return (domainName, config.ToDomainAuditConfiguration());
    }

    /// <summary>
    /// 根据实体类型获取领域名称
    /// </summary>
    private static string GetDomainNameFromEntity<TEntity>()
    {
        var entityType = typeof(TEntity);

        // // 根据实体类型映射到领域名称
        // if (entityType == typeof(Product)) return "Product";
        // if (entityType == typeof(Order)) return "Order";
        // if (entityType == typeof(SystemLog)) return "Log";
        // if (entityType == typeof(User)) return "System";

        // 默认使用实体类型名称
        return entityType.Name;
    }
}

/// <summary>
/// Lambda表达式配置类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public class LambdaConfig<TEntity>
{
    private List<string> _ignoredProperties = new List<string>();

    /// <summary>
    /// 是否启用审计
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 忽略属性（Lambda表达式）
    /// </summary>
    /// <param name="propertyExpression">属性表达式</param>
    /// <returns>当前配置实例</returns>
    public LambdaConfig<TEntity> Ignore(Expression<Func<TEntity, object>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        if ( !_ignoredProperties.Contains(propertyName) )
        {
            _ignoredProperties.Add(propertyName);
        }
        return this;
    }

    /// <summary>
    /// 忽略多个属性
    /// </summary>
    /// <param name="propertyNames">属性名称数组</param>
    /// <returns>当前配置实例</returns>
    public LambdaConfig<TEntity> Ignore(params string[] propertyNames)
    {
        foreach ( var propertyName in propertyNames )
        {
            if ( !string.IsNullOrEmpty(propertyName) && !_ignoredProperties.Contains(propertyName) )
            {
                _ignoredProperties.Add(propertyName);
            }
        }
        return this;
    }

    /// <summary>
    /// 转换为DomainAuditConfiguration
    /// </summary>
    /// <returns>DomainAuditConfiguration实例</returns>
    public DomainAuditConfiguration ToDomainAuditConfiguration()
    {
        var domainConfig = new DomainAuditConfiguration
        {
            Enabled = Enabled
        };

        domainConfig.IgnoredProperties = _ignoredProperties;

        return domainConfig;
    }

    /// <summary>
    /// 从Lambda表达式中提取属性名称
    /// </summary>
    private static string GetPropertyName(Expression<Func<TEntity, object>> propertyExpression)
    {
        if ( propertyExpression.Body is MemberExpression memberExpression )
        {
            return memberExpression.Member.Name;
        }

        if ( propertyExpression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression unaryMemberExpression )
        {
            return unaryMemberExpression.Member.Name;
        }

        throw new ArgumentException("无效的属性表达式", nameof(propertyExpression));
    }
}

/// <summary>
/// 配置扩展方法
/// </summary>
public static class AuditConfigurationExtensions
{
    /// <summary>
    /// 使用Lambda表达式配置领域
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="config">审计配置</param>
    /// <param name="lambdaConfig">Lambda配置</param>
    public static void ConfigureDomain<TEntity>(this object config,
        Action<LambdaConfig<TEntity>> lambdaConfig)
    {
        var (domainName, domainConfig) = AuditConfigBuilder.For(lambdaConfig);
        var method = config.GetType().GetMethod("ConfigureDomain", new[] { typeof(string), typeof(DomainAuditConfiguration) });
        method?.Invoke(config, new object[] { domainName, domainConfig });
    }
}
