using System.Text.RegularExpressions;

namespace Octopus.SearchCore.Ext;

public static class StringHelpers
{
    /// <summary>
    /// 移除字符串的指定字符
    /// </summary>
    /// <param name="s"></param>
    /// <param name="chars"></param>
    /// <returns></returns>
    internal static String RemoveCharacters(this String s, IEnumerable<Char> chars)
    {
        return String.IsNullOrEmpty(s) ? String.Empty : new String(s.Where(c => !chars.Contains(c)).ToArray());
    }

    /// <summary>
    /// 去除html标签后并截取字符串
    /// </summary>
    /// <param name="html">源html</param>
    /// <returns></returns>
    internal static String RemoveHtmlTag(this String html)
    {
        var strText = Regex.Replace(html, "<[^>]+>", "");
        strText = Regex.Replace(strText, "&[^;]+;", "");
        return strText;
    }

    /// <summary>
    /// 添加多个元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="values"></param>
    public static void AddRange<T>(this ICollection<T> @this, IEnumerable<T> values)
    {
        foreach (var obj in values)
        {
            @this.Add(obj);
        }
    }

    /// <summary>
    /// 移除符合条件的元素
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="where"></param>
    public static void RemoveWhere<T>(this ICollection<T> @this, Func<T, bool> @where)
    {
        foreach (var obj in @this.Where(where).ToList())
        {
            @this.Remove(obj);
        }
    }
}