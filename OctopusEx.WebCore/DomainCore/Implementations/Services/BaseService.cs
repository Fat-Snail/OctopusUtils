namespace OctopusEx.WebCore.DomainCore.Implementations.Services;

using Abstractions.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;

public abstract class BaseService : IAsyncBaseService
{
    /// <summary>
    /// 工作单元
    /// </summary>
    protected readonly IUnitOfWork UnitOfWork;

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger<BaseService>? Logger;

    /// <summary>
    /// 服务名称
    /// </summary>
    protected virtual string ServiceName => GetType().Name;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="unitOfWork">工作单元</param>
    /// <param name="logger">日志记录器（可选）</param>
    protected BaseService(IUnitOfWork unitOfWork, ILogger<BaseService>? logger = null)
    {
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        Logger = logger;
    }

    /// <summary>
    /// 获取服务名称
    /// </summary>
    /// <returns>服务名称</returns>
    public virtual string GetServiceName() => ServiceName;

    /// <summary>
    /// 初始化服务（异步）
    /// </summary>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 默认实现，子类可以重写
        Logger?.LogDebug("Service {ServiceName} initialized", ServiceName);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    protected virtual void LogInformation(string message, params object[] args)
    {
        Logger?.LogInformation(message, args);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    protected virtual void LogWarning(string message, params object[] args)
    {
        Logger?.LogWarning(message, args);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    protected virtual void LogError(Exception exception, string message, params object[] args)
    {
        Logger?.LogError(exception, message, args);
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="args">参数</param>
    protected virtual void LogDebug(string message, params object[] args)
    {
        Logger?.LogDebug(message, args);
    }
}
