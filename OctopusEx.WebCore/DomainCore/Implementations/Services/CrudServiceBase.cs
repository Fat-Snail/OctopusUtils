namespace OctopusEx.WebCore.DomainCore.Implementations.Services;

using System.Linq.Expressions;
using Abstractions.Interfaces.Services;
using APICommon;
using Microsoft.Extensions.Logging;
using Repositories.Interfaces;

/// <summary>
/// CRUD服务基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
public abstract class CrudServiceBase<TEntity, TKey, TDto, TCreateDto, TUpdateDto>
    : BaseService, ICrudService<TEntity, TKey, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TKey : notnull
{
    private readonly Lazy<IRepository<TEntity, TKey>> _repositoryLazy;

    /// <summary>
    /// 获取缓存的仓储实例
    /// </summary>
    protected virtual IRepository<TEntity, TKey> Repository => _repositoryLazy.Value;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="unitOfWork">工作单元</param>
    /// <param name="logger">日志记录器（可选）</param>
    protected CrudServiceBase(IUnitOfWork unitOfWork,
        ILogger<CrudServiceBase<TEntity, TKey, TDto, TCreateDto, TUpdateDto>>? logger = null)
        : base(unitOfWork, logger)
    {
        _repositoryLazy = new Lazy<IRepository<TEntity, TKey>>(() => unitOfWork.GetRepository<TEntity, TKey>());
    }


    #region 映射方法（子类应该根据需要重写）

    /// <summary>
    /// 将实体映射到DTO
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>DTO</returns>
    protected virtual TDto MapToDto(TEntity entity)
    {
        // 默认实现：使用反射或手动映射
        // 子类应该重写此方法以实现具体的映射逻辑
        // 建议使用AutoMapper或其他映射工具
        throw new NotImplementedException($"请重写 {nameof(MapToDto)} 方法以实现实体到DTO的映射");
    }

    /// <summary>
    /// 将DTO映射到实体（用于创建）
    /// </summary>
    /// <param name="createDto">创建DTO</param>
    /// <returns>实体</returns>
    protected virtual TEntity MapToEntity(TCreateDto createDto)
    {
        // 默认实现：使用反射或手动映射
        // 子类应该重写此方法以实现具体的映射逻辑
        throw new NotImplementedException($"请重写 {nameof(MapToEntity)} 方法以实现创建DTO到实体的映射");
    }

    /// <summary>
    /// 使用更新DTO更新实体
    /// </summary>
    /// <param name="entity">现有实体</param>
    /// <param name="updateDto">更新DTO</param>
    protected virtual void UpdateEntityFromDto(TEntity entity, TUpdateDto updateDto)
    {
        // 默认实现：使用反射或手动映射
        // 子类应该重写此方法以实现具体的更新逻辑
        throw new NotImplementedException($"请重写 {nameof(UpdateEntityFromDto)} 方法以实现从更新DTO更新实体");
    }

    /// <summary>
    /// 将实体列表映射到DTO列表
    /// </summary>
    /// <param name="entities">实体列表</param>
    /// <returns>DTO列表</returns>
    protected virtual List<TDto> MapToDtoList(List<TEntity> entities)
    {
        return entities.Select(MapToDto).ToList();
    }

    #endregion

    #region ICrudService 实现

    /// <summary>
    /// 根据主键获取单个实体（异步）
    /// </summary>
    public virtual async Task<TDto?> GetAsync(TKey id, CancellationToken cancellationToken = default)
    {
        LogDebug("开始获取实体，ID: {Id}", id);

        var entity = await Repository.GetByIdAsync(id, cancellationToken);

        if ( entity == null )
        {
            LogWarning("实体不存在，ID: {Id}", id);
            return default;
        }

        var dto = MapToDto(entity);
        LogDebug("成功获取实体，ID: {Id}", id);

        return dto;
    }

    /// <summary>
    /// 获取分页列表（异步）
    /// </summary>
    public virtual async Task<IPagedData<TDto>> GetListAsync(PageRequest request,
        CancellationToken cancellationToken = default)
    {
        LogDebug("开始获取分页列表，页码: {Page}, 每页大小: {PageSize}", request.Page, request.PageSize);

        var query = Repository.Find();

        // 应用过滤条件（由子类实现）
        query = ApplyListFilter(query, request);

        // 应用排序（由子类实现）
        query = ApplyListSorting(query, request);

        // 获取总数
        var totalCount =
            await Repository.CountAsync(query.Expression as Expression<Func<TEntity, bool>>, cancellationToken);

        // 分页
        var validPage = request.GetValidPage();
        var validPageSize = request.GetValidPageSize();
        var skipCount = (validPage - 1) * validPageSize;

        var entities = await Repository.FindAllAsync(
            condition: query.Expression as Expression<Func<TEntity, bool>>,
            orderBy: q => query as IOrderedQueryable<TEntity> ?? q.OrderBy(x => 1),
            cancellationToken: cancellationToken);

        // 转换为DTO
        var dtos = MapToDtoList(entities);

        LogDebug("成功获取分页列表，总数: {TotalCount}, 当前页记录数: {ItemCount}", totalCount, dtos.Count);

        var totalPages = ( int )Math.Ceiling(( double )totalCount / validPageSize);

        return new PagedData<TDto>
        {
            Page = validPage,
            PageSize = validPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = dtos
        };
    }

    /// <summary>
    /// 获取所有记录（异步）
    /// </summary>
    public virtual async Task<List<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        LogDebug("开始获取所有记录");

        var entities = await Repository.GetAllAsync(cancellationToken);
        var dtos = MapToDtoList(entities);

        LogDebug("成功获取所有记录，总数: {Count}", dtos.Count);

        return dtos;
    }

    /// <summary>
    /// 根据主键列表获取多个实体（异步）
    /// </summary>
    public virtual async Task<List<TDto>> GetByIdsAsync(IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        LogDebug("开始批量获取实体，ID数量: {Count}", idList.Count);

        var entities = await Repository.GetByIdsAsync(idList, cancellationToken);
        var dtos = MapToDtoList(entities);

        LogDebug("成功批量获取实体，返回记录数: {Count}", dtos.Count);

        return dtos;
    }

    /// <summary>
    /// 创建新实体（异步）
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateDto request, CancellationToken cancellationToken = default)
    {
        LogDebug("开始创建新实体");

        // 验证请求（由子类实现）
        var validationResult = ValidateCreateRequest(request);
        if ( !validationResult.IsValid )
        {
            throw new ArgumentException(validationResult.ErrorMessage);
        }

        // 映射为实体
        var entity = MapToEntity(request);

        // 创建前处理（由子类实现）
        await BeforeCreateAsync(entity, request, cancellationToken);

        // 保存到数据库
        await Repository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        // 创建后处理（由子类实现）
        await AfterCreateAsync(entity, request, cancellationToken);

        // 转换为DTO返回
        var dto = MapToDto(entity);
        LogDebug("成功创建新实体，ID: {Id}", GetEntityId(entity));

        return dto;
    }

    /// <summary>
    /// 更新实体（异步）
    /// </summary>
    public virtual async Task<TDto> UpdateAsync(TUpdateDto request, CancellationToken cancellationToken = default)
    {
        LogDebug("开始更新实体");

        // 验证请求（由子类实现）
        var validationResult = ValidateUpdateRequest(request);
        if ( !validationResult.IsValid )
        {
            throw new ArgumentException(validationResult.ErrorMessage);
        }

        // 获取主键（由子类实现）
        var id = GetUpdateRequestId(request);

        // 查找现有实体
        var entity = await Repository.GetByIdAsync(id, cancellationToken);

        if ( entity == null )
        {
            LogWarning("要更新的实体不存在，ID: {Id}", id);
            throw new KeyNotFoundException($"ID为 {id} 的记录不存在");
        }

        // 更新前处理（由子类实现）
        await BeforeUpdateAsync(entity, request, cancellationToken);

        // 更新实体
        UpdateEntityFromDto(entity, request);

        // 保存更改
        await Repository.UpdateAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        // 更新后处理（由子类实现）
        await AfterUpdateAsync(entity, request, cancellationToken);

        // 转换为DTO返回
        var dto = MapToDto(entity);
        LogDebug("成功更新实体，ID: {Id}", id);

        return dto;
    }

    /// <summary>
    /// 根据主键删除实体（异步）
    /// </summary>
    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        LogDebug("开始删除实体，ID: {Id}", id);

        // 删除前检查（由子类实现）
        var canDeleteResult = await CanDeleteAsync(id, cancellationToken);
        if ( !canDeleteResult.CanDelete )
        {
            throw new InvalidOperationException(canDeleteResult.Reason ?? "不能删除该记录");
        }

        // 执行删除
        var deleted = await Repository.DeleteByIdAsync(id, cancellationToken);

        if ( !deleted )
        {
            LogWarning("要删除的实体不存在，ID: {Id}", id);
            return false;
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        LogDebug("成功删除实体，ID: {Id}", id);
        return true;
    }

    /// <summary>
    /// 批量删除实体（异步）
    /// </summary>
    public virtual async Task<int> DeleteBatchAsync(IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        LogDebug("开始批量删除实体，ID数量: {Count}", idList.Count);

        if ( idList.Count == 0 )
        {
            return 0;
        }

        // 批量删除前检查（由子类实现）
        var canDeleteResult = await CanDeleteBatchAsync(idList, cancellationToken);
        if ( !canDeleteResult.CanDelete )
        {
            throw new InvalidOperationException(canDeleteResult.Reason ?? "不能删除这些记录");
        }

        // 执行批量删除
        var deletedCount = 0;

        foreach ( var id in idList )
        {
            var deleted = await Repository.DeleteByIdAsync(id, cancellationToken);
            if ( deleted )
            {
                deletedCount++;
            }
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        LogDebug("成功批量删除实体，删除数量: {DeletedCount}", deletedCount);
        return deletedCount;
    }

    /// <summary>
    /// 检查实体是否存在（异步）
    /// </summary>
    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        try
        {
            var repository = Repository;
            var entity = await repository.GetByIdAsync(id, cancellationToken);
            return entity != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 可重写的方法（用于扩展和自定义）

    /// <summary>
    /// 应用列表查询过滤条件
    /// </summary>
    /// <param name="query">查询对象</param>
    /// <param name="request">分页请求</param>
    /// <returns>过滤后的查询</returns>
    protected virtual IQueryable<TEntity> ApplyListFilter(IQueryable<TEntity> query, PageRequest request)
    {
        // 默认实现：不应用过滤
        // 子类可以根据request中的条件应用过滤
        return query;
    }

    /// <summary>
    /// 应用列表排序
    /// </summary>
    /// <param name="query">查询对象</param>
    /// <param name="request">分页请求</param>
    /// <returns>排序后的查询</returns>
    protected virtual IQueryable<TEntity> ApplyListSorting(IQueryable<TEntity> query, PageRequest request)
    {
        // 默认实现：按默认方式排序
        // 子类可以根据request中的排序字段应用排序
        return query;
    }

    /// <summary>
    /// 验证创建请求
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>验证结果</returns>
    protected virtual ValidationResult ValidateCreateRequest(TCreateDto request)
    {
        // 默认实现：验证通过
        return ValidationResult.Success;
    }

    /// <summary>
    /// 验证更新请求
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>验证结果</returns>
    protected virtual ValidationResult ValidateUpdateRequest(TUpdateDto request)
    {
        // 默认实现：验证通过
        return ValidationResult.Success;
    }

    /// <summary>
    /// 从更新请求中获取主键
    /// </summary>
    /// <param name="request">更新请求</param>
    /// <returns>主键值</returns>
    protected abstract TKey GetUpdateRequestId(TUpdateDto request);

    /// <summary>
    /// 获取实体的主键值
    /// </summary>
    /// <param name="entity">实体</param>
    /// <returns>主键值</returns>
    protected abstract TKey GetEntityId(TEntity entity);

    /// <summary>
    /// 创建前处理
    /// </summary>
    /// <param name="entity">要创建的实体</param>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeCreateAsync(TEntity entity, TCreateDto request,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：不做处理
        return Task.CompletedTask;
    }

    /// <summary>
    /// 创建后处理
    /// </summary>
    /// <param name="entity">已创建的实体</param>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterCreateAsync(TEntity entity, TCreateDto request,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：不做处理
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新前处理
    /// </summary>
    /// <param name="entity">要更新的实体</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeUpdateAsync(TEntity entity, TUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：不做处理
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新后处理
    /// </summary>
    /// <param name="entity">已更新的实体</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterUpdateAsync(TEntity entity, TUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：不做处理
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查是否可以删除
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除检查结果</returns>
    protected virtual Task<DeleteCheckResult> CanDeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // 默认实现：可以删除
        return Task.FromResult(DeleteCheckResult.Allowed);
    }

    /// <summary>
    /// 检查是否可以批量删除
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除检查结果</returns>
    protected virtual Task<DeleteCheckResult> CanDeleteBatchAsync(List<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        // 默认实现：可以删除
        return Task.FromResult(DeleteCheckResult.Allowed);
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ValidationResult Success => new ValidationResult { IsValid = true };

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>验证结果</returns>
        public static ValidationResult Fail(string errorMessage) => new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 删除检查结果
    /// </summary>
    public class DeleteCheckResult
    {
        /// <summary>
        /// 是否可以删除
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 不能删除的原因
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// 允许删除的结果
        /// </summary>
        public static DeleteCheckResult Allowed => new DeleteCheckResult { CanDelete = true };

        /// <summary>
        /// 不允许删除的结果
        /// </summary>
        /// <param name="reason">原因</param>
        /// <returns>删除检查结果</returns>
        public static DeleteCheckResult NotAllowed(string reason) => new DeleteCheckResult
        {
            CanDelete = false,
            Reason = reason
        };
    }

    #endregion
}
