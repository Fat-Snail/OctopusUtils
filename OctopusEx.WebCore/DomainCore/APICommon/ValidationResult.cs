namespace OctopusEx.WebCore.DomainCore.APICommon;

using System.Text.Json.Serialization;

/// <summary>
/// 验证结果（共享类型）。
/// 同时保留 <see cref="IsSuccess"/> 与 <see cref="IsValid"/> 两个属性以兼容
/// Controller 端（原使用 <c>IsSuccess</c>）和 Service 端（原使用 <c>IsValid</c>）的调用方。
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 是否验证通过（Controller 端原始字段名）。
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 是否验证通过（Service 端原始字段名，作为 <see cref="IsSuccess"/> 的别名）。
    /// 标记 <see cref="JsonIgnoreAttribute"/> 以保证 Controller 通过 BadRequest 返回的
    /// JSON 结构与重构前完全一致（仅含 isSuccess + errorMessage），不因合并类型而新增字段。
    /// Service 端仅在内存中使用 IsValid，不参与序列化，故忽略无副作用。
    /// </summary>
    [JsonIgnore]
    public bool IsValid
    {
        get => IsSuccess;
        set => IsSuccess = value;
    }

    /// <summary>
    /// 错误消息。
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建成功的验证结果。
    /// </summary>
    public static ValidationResult Success => new ValidationResult { IsSuccess = true };

    /// <summary>
    /// 创建失败的验证结果。
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <returns>验证结果</returns>
    public static ValidationResult Fail(string errorMessage) => new ValidationResult
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
