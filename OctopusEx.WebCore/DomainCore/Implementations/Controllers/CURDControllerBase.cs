using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OctopusEx.WebCore.DomainCore.Abstractions.Interfaces.Services;
using OctopusEx.WebCore.DomainCore.APICommon;

namespace OctopusEx.WebCore.DomainCore.Implementations.Controllers;

/// <summary>
/// 通用CRUD控制器基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TDto">DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
[ApiController]
[Route("api/[controller]")]
public abstract class CURDControllerBase<TEntity, TKey, TDto, TCreateDto, TUpdateDto> : ControllerBase
    where TEntity : class
    where TKey : notnull
{
    private readonly ICrudService<TEntity, TKey, TDto, TCreateDto, TUpdateDto> _service;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="service">CRUD服务</param>
    protected CURDControllerBase(ICrudService<TEntity, TKey, TDto, TCreateDto, TUpdateDto> service)
    {
        _service = service;
    }

    /// <summary>
    /// 获取CRUD服务实例（保护属性，子类可访问）
    /// </summary>
    protected ICrudService<TEntity, TKey, TDto, TCreateDto, TUpdateDto> Service => _service;

    #region 查询操作

    /// <summary>
    /// 根据主键获取单个实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体DTO</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<BaseResponse<TDto>>> GetAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _service.GetAsync(id, cancellationToken);
            if ( entity == null )
            {
                return NotFound(BaseResponse<TDto>.Error("记录不存在"));
            }

            return Ok(BaseResponse<TDto>.Success(entity, "获取成功"));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页数据</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<BaseResponsePaged<TDto>>> GetPagedListAsync(
        [FromQuery] PageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pagedData = await _service.GetListAsync(request, cancellationToken);
            return Ok(BaseResponsePaged<TDto>.Success(pagedData, "获取成功"));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 获取所有记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有记录列表</returns>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<BaseResponse<List<TDto>>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _service.GetAllAsync(cancellationToken);
            return Ok(BaseResponse<List<TDto>>.Success(entities, "获取成功"));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 根据主键列表获取多个实体
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    [HttpGet("by-ids")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<BaseResponse<List<TDto>>>> GetByIdsAsync(
        [FromQuery] IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _service.GetByIdsAsync(ids, cancellationToken);
            return Ok(BaseResponse<List<TDto>>.Success(entities, "获取成功"));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    [HttpGet("exists/{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<BaseResponse<bool>>> ExistsAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _service.ExistsAsync(id, cancellationToken);
            return Ok(BaseResponse<bool>.Success(exists, exists ? "记录存在" : "记录不存在"));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    #endregion

    #region 创建操作

    /// <summary>
    /// 创建新实体
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的实体</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<BaseResponse<TDto>>> CreateAsync(
        [FromBody] TCreateDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 创建前验证
            var validationResult = await ValidateCreateRequestAsync(request, cancellationToken);
            if ( !validationResult.IsSuccess )
            {
                return BadRequest(validationResult);
            }

            // 创建前处理
            await BeforeCreateAsync(request, cancellationToken);

            // 执行创建
            var createdEntity = await _service.CreateAsync(request, cancellationToken);

            // 创建后处理
            await AfterCreateAsync(createdEntity, request, cancellationToken);

            // 获取创建后的实体ID
            var entityId = GetEntityIdFromDto(createdEntity);

            return CreatedAtAction(nameof(GetAsync), new { id = entityId },
                BaseResponse<TDto>.Success(createdEntity, "创建成功"));
        }
        catch ( ArgumentException ex )
        {
            return BadRequest(BaseResponse<TDto>.Error(ex.Message));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    #endregion

    #region 更新操作

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的实体</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<BaseResponse<TDto>>> UpdateAsync(
        TKey id,
        [FromBody] TUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 更新前验证
            var validationResult = await ValidateUpdateRequestAsync(id, request, cancellationToken);
            if ( !validationResult.IsSuccess )
            {
                return BadRequest(validationResult);
            }

            // 更新前处理
            await BeforeUpdateAsync(id, request, cancellationToken);

            // 执行更新
            var updatedEntity = await _service.UpdateAsync(request, cancellationToken);

            // 更新后处理
            await AfterUpdateAsync(updatedEntity, request, cancellationToken);

            return Ok(BaseResponse<TDto>.Success(updatedEntity, "更新成功"));
        }
        catch ( KeyNotFoundException ex )
        {
            return NotFound(BaseResponse<TDto>.Error(ex.Message));
        }
        catch ( ArgumentException ex )
        {
            return BadRequest(BaseResponse<TDto>.Error(ex.Message));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    #endregion

    #region 删除操作

    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<BaseResponse<bool>>> DeleteAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 删除前检查
            var canDeleteResult = await CanDeleteAsync(id, cancellationToken);
            if ( !canDeleteResult.CanDelete )
            {
                return BadRequest(BaseResponse<bool>.Error(canDeleteResult.Reason ?? "不能删除该记录"));
            }

            // 删除前处理
            await BeforeDeleteAsync(id, cancellationToken);

            // 执行删除
            var deleted = await _service.DeleteAsync(id, cancellationToken);

            // 删除后处理
            await AfterDeleteAsync(id, deleted, cancellationToken);

            if ( !deleted )
            {
                return NotFound(BaseResponse<bool>.Error("记录不存在"));
            }

            return Ok(BaseResponse<bool>.Success(true, "删除成功"));
        }
        catch ( InvalidOperationException ex )
        {
            return BadRequest(BaseResponse<bool>.Error(ex.Message));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 批量删除实体
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除数量</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse), StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<BaseResponse<int>>> DeleteBatchAsync(
        [FromBody] IEnumerable<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var idList = ids.ToList();
            if ( idList.Count == 0 )
            {
                return Ok(BaseResponse<int>.Success(0, "未提供要删除的记录"));
            }

            // 批量删除前检查
            var canDeleteResult = await CanDeleteBatchAsync(idList, cancellationToken);
            if ( !canDeleteResult.CanDelete )
            {
                return BadRequest(BaseResponse<int>.Error(canDeleteResult.Reason ?? "不能删除这些记录"));
            }

            // 批量删除前处理
            await BeforeDeleteBatchAsync(idList, cancellationToken);

            // 执行批量删除
            var deletedCount = await _service.DeleteBatchAsync(idList, cancellationToken);

            // 批量删除后处理
            await AfterDeleteBatchAsync(idList, deletedCount, cancellationToken);

            return Ok(BaseResponse<int>.Success(deletedCount, $"成功删除 {deletedCount} 条记录"));
        }
        catch ( InvalidOperationException ex )
        {
            return BadRequest(BaseResponse<int>.Error(ex.Message));
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    #endregion

    #region 可重写的方法（用于扩展和自定义）

    /// <summary>
    /// 从DTO中获取实体主键值（子类必须实现）
    /// </summary>
    /// <param name="dto">DTO</param>
    /// <returns>主键值</returns>
    protected abstract TKey GetEntityIdFromDto(TDto dto);

    /// <summary>
    /// 验证创建请求（子类可重写）
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    protected virtual Task<ValidationResult> ValidateCreateRequestAsync(TCreateDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success);
    }

    /// <summary>
    /// 验证更新请求（子类可重写）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    protected virtual Task<ValidationResult> ValidateUpdateRequestAsync(TKey id, TUpdateDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success);
    }

    /// <summary>
    /// 创建前处理（子类可重写）
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeCreateAsync(TCreateDto request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 创建后处理（子类可重写）
    /// </summary>
    /// <param name="createdEntity">已创建的实体</param>
    /// <param name="request">创建请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterCreateAsync(TDto createdEntity, TCreateDto request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新前处理（子类可重写）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeUpdateAsync(TKey id, TUpdateDto request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 更新后处理（子类可重写）
    /// </summary>
    /// <param name="updatedEntity">已更新的实体</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterUpdateAsync(TDto updatedEntity, TUpdateDto request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检查是否可以删除（子类可重写）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除检查结果</returns>
    protected virtual Task<DeleteCheckResult> CanDeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteCheckResult.Allowed);
    }

    /// <summary>
    /// 检查是否可以批量删除（子类可重写）
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除检查结果</returns>
    protected virtual Task<DeleteCheckResult> CanDeleteBatchAsync(List<TKey> ids, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeleteCheckResult.Allowed);
    }

    /// <summary>
    /// 删除前处理（子类可重写）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeDeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除后处理（子类可重写）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="deleted">是否成功删除</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterDeleteAsync(TKey id, bool deleted, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量删除前处理（子类可重写）
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task BeforeDeleteBatchAsync(List<TKey> ids, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 批量删除后处理（子类可重写）
    /// </summary>
    /// <param name="ids">主键列表</param>
    /// <param name="deletedCount">成功删除的数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    protected virtual Task AfterDeleteBatchAsync(List<TKey> ids, int deletedCount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 处理异常，返回统一的错误响应
    /// </summary>
    /// <param name="ex">异常</param>
    /// <returns>错误响应</returns>
    protected virtual ActionResult HandleException(Exception ex)
    {
        // 这里可以根据异常类型返回不同的状态码
        // 默认返回500内部服务器错误
        return StatusCode(StatusCodes.Status500InternalServerError,
            BaseResponse.Error($"服务器内部错误: {ex.Message}"));
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
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 创建成功的验证结果
        /// </summary>
        public static ValidationResult Success => new ValidationResult { IsSuccess = true };

        /// <summary>
        /// 创建失败的验证结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <returns>验证结果</returns>
        public static ValidationResult Fail(string errorMessage) => new ValidationResult
        {
            IsSuccess = false,
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

/// <summary>
/// 简化版CRUD控制器基类（当TCreateDto和TUpdateDto与TDto相同时使用）
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TDto">DTO类型（同时作为创建和更新DTO）</typeparam>
[ApiController]
[Route("api/[controller]")]
public abstract class CURDControllerBase<TEntity, TKey, TDto> : CURDControllerBase<TEntity, TKey, TDto, TDto, TDto>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="service">CRUD服务</param>
    protected CURDControllerBase(ICrudService<TEntity, TKey, TDto, TDto, TDto> service) : base(service)
    {
    }
}
