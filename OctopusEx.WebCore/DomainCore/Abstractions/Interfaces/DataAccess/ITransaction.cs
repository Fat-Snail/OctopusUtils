using System;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 事务接口
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
