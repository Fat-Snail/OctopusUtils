using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OctopusEx.WebCore.DomainCore.Abstractions.Interfaces.Services;
using OctopusEx.WebCore.DomainCore.APICommon;
using ValidationResult = OctopusEx.WebCore.DomainCore.APICommon.ValidationResult;
using DeleteCheckResult = OctopusEx.WebCore.DomainCore.APICommon.DeleteCheckResult;

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
        return await ExecuteAsync<BaseResponse<TDto>>(async () =>
        {
            var entity = await _service.GetAsync(id, cancellationToken);
            if ( entity == null )
            {
                return NotFound(BaseResponse<TDto>.Error("记录不存在"));
            }

            return Ok(BaseResponse<TDto>.Success(entity, "获取成功"));
        });
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
        return await ExecuteAsync<BaseResponsePaged<TDto>>(async () =>
        {
            var pagedData = await _service.GetListAsync(request, cancellationToken);
            return Ok(BaseResponsePaged<TDto>.Success(pagedData, "获取成功"));
        });
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
        return await ExecuteAsync<BaseResponse<List<TDto>>>(async () =>
        {
            var entities = await _service.GetAllAsync(cancellationToken);
            return Ok(BaseResponse<List<TDto>>.Success(entities, "获取成功"));
        });
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
        return await ExecuteAsync<BaseResponse<List<TDto>>>(async () =>
        {
            var entities = await _service.GetByIdsAsync(ids, cancellationToken);
            return Ok(BaseResponse<List<TDto>>.Success(entities, "获取成功"));
        });
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
        return await ExecuteAsync<BaseResponse<bool>>(async () =>
        {
            var exists = await _service.ExistsAsync(id, cancellationToken);
            return Ok(BaseResponse<bool>.Success(exists, exists ? "记录存在" : "记录不存在"));
        });
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
        catch ( Exception ex )
        {
            return HandleWriteException<TDto>(ex);
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

            return OkResponse(updatedEntity, "更新成功");
        }
        catch ( Exception ex )
        {
            return HandleWriteException<TDto>(ex);
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
                return BadRequestResponse<bool>(canDeleteResult.Reason ?? "不能删除该记录");
            }

            // 删除前处理
            await BeforeDeleteAsync(id, cancellationToken);

            // 执行删除
            var deleted = await _service.DeleteAsync(id, cancellationToken);

            // 删除后处理
            await AfterDeleteAsync(id, deleted, cancellationToken);

            if ( !deleted )
            {
                return NotFoundResponse<bool>("记录不存在");
            }

            return OkResponse(true, "删除成功");
        }
        catch ( Exception ex )
        {
            return HandleWriteException<bool>(ex);
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
                return OkResponse(0, "未提供要删除的记录");
            }

            // 批量删除前检查
            var canDeleteResult = await CanDeleteBatchAsync(idList, cancellationToken);
            if ( !canDeleteResult.CanDelete )
            {
                return BadRequestResponse<int>(canDeleteResult.Reason ?? "不能删除这些记录");
            }

            // 批量删除前处理
            await BeforeDeleteBatchAsync(idList, cancellationToken);

            // 执行批量删除
            var deletedCount = await _service.DeleteBatchAsync(idList, cancellationToken);

            // 批量删除后处理
            await AfterDeleteBatchAsync(idList, deletedCount, cancellationToken);

            return OkResponse(deletedCount, $"成功删除 {deletedCount} 条记录");
        }
        catch ( Exception ex )
        {
            return HandleWriteException<int>(ex);
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

    /// <summary>
    /// 执行异步操作并统一处理异常（封装 try-catch 模式）。
    /// 适用于查询类方法：捕获所有异常并委托给 <see cref="HandleException"/> 处理。
    /// </summary>
    /// <typeparam name="T">ActionResult 内部的数据类型（如 <see cref="BaseResponse{TDto}"/>）</typeparam>
    /// <param name="action">要执行的异步操作，返回具体的 <see cref="ActionResult{T}"/></param>
    /// <returns>操作结果或异常处理后的错误响应</returns>
    protected async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<ActionResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch ( Exception ex )
        {
            return HandleException(ex);
        }
    }

    /// <summary>
    /// 处理写操作异常，将常见异常类型统一映射到对应的 HTTP 响应。
    /// <list type="bullet">
    ///   <item><see cref="KeyNotFoundException"/> → 404 NotFound</item>
    ///   <item><see cref="ArgumentException"/> → 400 BadRequest</item>
    ///   <item><see cref="InvalidOperationException"/> → 400 BadRequest</item>
    ///   <item>其他异常 → 委托给 <see cref="HandleException"/></item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">BaseResponse 内部的数据类型</typeparam>
    /// <param name="ex">捕获的异常</param>
    /// <returns>映射后的 HTTP 响应</returns>
    protected ActionResult<BaseResponse<T>> HandleWriteException<T>(Exception ex)
    {
        if ( ex is KeyNotFoundException )
        {
            return NotFound(BaseResponse<T>.Error(ex.Message));
        }

        if ( ex is ArgumentException )
        {
            return BadRequest(BaseResponse<T>.Error(ex.Message));
        }

        if ( ex is InvalidOperationException )
        {
            return BadRequest(BaseResponse<T>.Error(ex.Message));
        }

        return HandleException(ex);
    }

    /// <summary>
    /// 构造 200 OK 成功响应。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="data">响应数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>成功的 ActionResult</returns>
    protected ActionResult<BaseResponse<T>> OkResponse<T>(T data, string message = "操作成功")
    {
        ActionResult result = Ok(BaseResponse<T>.Success(data, message));
        return result;
    }

    /// <summary>
    /// 构造 404 NotFound 错误响应。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>未找到的 ActionResult</returns>
    protected ActionResult<BaseResponse<T>> NotFoundResponse<T>(string message)
    {
        ActionResult result = NotFound(BaseResponse<T>.Error(message));
        return result;
    }

    /// <summary>
    /// 构造 400 BadRequest 错误响应。
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="message">错误消息</param>
    /// <returns>错误请求的 ActionResult</returns>
    protected ActionResult<BaseResponse<T>> BadRequestResponse<T>(string message)
    {
        ActionResult result = BadRequest(BaseResponse<T>.Error(message));
        return result;
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
