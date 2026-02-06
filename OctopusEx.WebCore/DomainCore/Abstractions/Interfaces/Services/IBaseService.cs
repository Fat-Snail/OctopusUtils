namespace OctopusEx.WebCore.DomainCore.Abstractions.Interfaces.Services;

public interface IBaseService
{
    /// <summary>
    /// 获取服务名称
    /// </summary>
    /// <returns>服务名称</returns>
    string GetServiceName();
}

/// <summary>
/// 异步基础服务接口
/// </summary>
public interface IAsyncBaseService : IBaseService
{
    /// <summary>
    /// 初始化服务（异步）
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
