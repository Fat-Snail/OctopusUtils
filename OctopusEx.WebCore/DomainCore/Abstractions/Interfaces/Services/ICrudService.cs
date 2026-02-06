namespace OctopusEx.WebCore.DomainCore.Abstractions.Interfaces.Services;

using APICommon;

/// <summary>
/// 基础CRUD服务接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
public interface ICrudService<TEntity, TKey, TDto> : IAsyncBaseService
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// 根据主键获取单个实体（异步）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体DTO，如果不存在则返回null</returns>
    Task<TDto?> GetAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分页列表（异步）
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页数据</returns>
    Task<IPagedData<TDto>> GetListAsync(PageRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有记录（异步）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体DTO列表</returns>
    Task<List<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键列表获取多个实体（异步）
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体DTO列表</returns>
    Task<List<TDto>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// 完整的CRUD服务接口（包含创建、更新、删除）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
public interface ICrudService<TEntity, TKey, TDto, TCreateDto, TUpdateDto>
    : ICrudService<TEntity, TKey, TDto>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// 创建新实体（异步）
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的实体DTO</returns>
    Task<TDto> CreateAsync(TCreateDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（异步）
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的实体DTO</returns>
    Task<TDto> UpdateAsync(TUpdateDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键删除实体（异步）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功删除（true表示成功，false表示记录不存在）</returns>
    Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体（异步）
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>成功删除的记录数</returns>
    Task<int> DeleteBatchAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查实体是否存在（异步）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
}
