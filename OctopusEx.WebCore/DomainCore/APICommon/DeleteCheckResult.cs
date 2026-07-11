namespace OctopusEx.WebCore.DomainCore.APICommon;

/// <summary>
/// 删除检查结果（共享类型）。
/// 从 Controller 与 Service 基类的同名嵌套类中提取，消除重复定义。
/// </summary>
public class DeleteCheckResult
{
    /// <summary>
    /// 是否可以删除。
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// 不能删除的原因。
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// 允许删除的结果。
    /// </summary>
    public static DeleteCheckResult Allowed => new DeleteCheckResult { CanDelete = true };

    /// <summary>
    /// 不允许删除的结果。
    /// </summary>
    /// <param name="reason">原因</param>
    /// <returns>删除检查结果</returns>
    public static DeleteCheckResult NotAllowed(string reason) => new DeleteCheckResult
    {
        CanDelete = false,
        Reason = reason
    };
}
