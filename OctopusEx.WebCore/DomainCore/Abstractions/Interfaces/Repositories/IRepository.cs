using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 泛型仓储接口（同时继承查询和命令接口，保持向后兼容）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> : IQuery<TEntity, TKey>, ICommand<TEntity, TKey> where TEntity : class
{
    // 注意：所有方法已通过IQuery和ICommand接口提供
    // 这里可以添加特定于IRepository的额外方法（如果需要）
}
