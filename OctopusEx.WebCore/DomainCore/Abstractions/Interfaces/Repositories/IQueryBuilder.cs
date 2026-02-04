using System;
using System.Linq;
using System.Linq.Expressions;

namespace OctopusEx.WebCore.Repositories.Interfaces;

/// <summary>
/// 查询构建器接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
public interface IQueryBuilder<TEntity> where TEntity : class
{
    /// <summary>
    /// 添加筛选条件
    /// </summary>
    IQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 添加排序（升序）
    /// </summary>
    IQueryBuilder<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// 添加排序（降序）
    /// </summary>
    IQueryBuilder<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// 添加ThenBy排序（升序）
    /// </summary>
    IQueryBuilder<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// 添加ThenBy排序（降序）
    /// </summary>
    IQueryBuilder<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// 包含关联实体
    /// </summary>
    IQueryBuilder<TEntity> Include<TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath);

    /// <summary>
    /// 包含多个关联实体
    /// </summary>
    IQueryBuilder<TEntity> Include(params Expression<Func<TEntity, object>>[] navigationPropertyPaths);

    /// <summary>
    /// 限制结果数量
    /// </summary>
    IQueryBuilder<TEntity> Take(int limit);

    /// <summary>
    /// 跳过指定数量的结果
    /// </summary>
    IQueryBuilder<TEntity> Skip(int count);

    /// <summary>
    /// 启用无跟踪查询
    /// </summary>
    IQueryBuilder<TEntity> AsNoTracking();

    /// <summary>
    /// 获取查询表达式
    /// </summary>
    IQueryable<TEntity> Build();
}
