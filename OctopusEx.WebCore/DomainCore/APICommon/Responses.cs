namespace OctopusEx.WebCore.DomainCore.APICommon;

using System.Text.Json.Serialization;

/// <summary>
/// 基础API响应模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class BaseResponse<T>
{
    /// <summary>
    /// 响应码（0表示成功，其他为错误码）
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// 响应时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 创建一个成功的响应
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>成功的响应</returns>
    public static BaseResponse<T> Success(T data, string message = "操作成功")
    {
        return new BaseResponse<T> { Code = 0, Message = message, Data = data };
    }

    /// <summary>
    /// 创建一个失败的响应
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败的响应</returns>
    public static BaseResponse<T> Error(int code, string message = "操作失败")
    {
        return new BaseResponse<T> { Code = code, Message = message, Data = default };
    }

    /// <summary>
    /// 创建一个失败的响应（使用默认错误码-1）
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>失败的响应</returns>
    public static BaseResponse<T> Error(string message = "操作失败")
    {
        return Error(-1, message);
    }

    /// <summary>
    /// 判断响应是否成功
    /// </summary>
    /// <returns>是否成功</returns>
    public bool IsSuccess() => Code == 0;
}

/// <summary>
/// 无数据的API响应模型
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// 响应码（0表示成功，其他为错误码）
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应时间戳
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 链路追踪 ID（仅在错误响应中填充，方便排查）
    /// </summary>
    [JsonPropertyName("traceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public String? TraceId { get; set; }

    /// <summary>
    /// 字段级错误（仅在表单验证失败 422 时填充）
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<String, String[]>? Errors { get; set; }

    /// <summary>
    /// 创建一个成功的响应
    /// </summary>
    /// <param name="message">响应消息</param>
    /// <returns>成功的响应</returns>
    public static BaseResponse Success(string message = "操作成功")
    {
        return new BaseResponse { Code = 0, Message = message };
    }

    /// <summary>
    /// 创建一个失败的响应
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败的响应</returns>
    public static BaseResponse Error(int code, string message = "操作失败")
    {
        return new BaseResponse { Code = code, Message = message };
    }

    /// <summary>
    /// 创建一个失败的响应（使用默认错误码-1）
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>失败的响应</returns>
    public static BaseResponse Error(string message = "操作失败")
    {
        return Error(-1, message);
    }

    /// <summary>
    /// 判断响应是否成功
    /// </summary>
    /// <returns>是否成功</returns>
    public bool IsSuccess() => Code == 0;
}

/// <summary>
/// 分页API响应模型
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class BaseResponsePaged<T> : BaseResponse<IPagedData<T>>
{
    /// <summary>
    /// 创建一个成功的分页响应
    /// </summary>
    /// <param name="data">分页数据</param>
    /// <param name="message">响应消息</param>
    /// <returns>成功的分页响应</returns>
    public static new BaseResponsePaged<T> Success(IPagedData<T> data, string message = "操作成功")
    {
        return new BaseResponsePaged<T> { Code = 0, Message = message, Data = data };
    }

    /// <summary>
    /// 创建一个失败的分页响应
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败的分页响应</returns>
    public static new BaseResponsePaged<T> Error(int code, string message = "操作失败")
    {
        return new BaseResponsePaged<T> { Code = code, Message = message, Data = default };
    }

    /// <summary>
    /// 创建一个失败的分页响应（使用默认错误码-1）
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <returns>失败的分页响应</returns>
    public static new BaseResponsePaged<T> Error(string message = "操作失败")
    {
        return Error(-1, message);
    }

    /// <summary>
    /// 快速创建分页响应
    /// </summary>
    /// <param name="items">数据项列表</param>
    /// <param name="page">当前页码</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="totalCount">总记录数</param>
    /// <param name="message">响应消息</param>
    /// <returns>分页响应</returns>
    public static BaseResponsePaged<T> Create(
        List<T> items,
        int page,
        int pageSize,
        long totalCount,
        string message = "操作成功")
    {
        var pagedData = new PagedData<T>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = pageSize > 0 ? ( int )Math.Ceiling(totalCount / ( double )pageSize) : 0,
            Items = items
        };

        return Success(pagedData, message);
    }
}
