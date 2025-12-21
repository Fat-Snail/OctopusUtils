namespace Util.Helpers;

/// <summary>
/// 验证扩展
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 检测对象是否为null,为null则抛出<see cref="ArgumentNullException"/>异常
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="parameterName">参数名</param>
    public static void CheckNull(this Object obj, String parameterName)
    {
        if ( obj == null )
            throw new ArgumentNullException(parameterName);
    }
}
