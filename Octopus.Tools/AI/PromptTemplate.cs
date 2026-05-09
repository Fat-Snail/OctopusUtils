namespace Octopus.AI;

using System.Text.RegularExpressions;

/// <summary>
/// 简易 Prompt 模板引擎。占位符语法：<c>{name}</c>。
/// 不做表达式解析，只做字面量替换；嵌套或条件分支请用代码组装。
/// </summary>
public class PromptTemplate
{
    private static readonly Regex PlaceholderRegex = new(@"\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);

    private readonly String _template;

    public PromptTemplate(String template) => _template = template ?? throw new ArgumentNullException(nameof(template));

    /// <summary>
    /// 用变量字典渲染模板。未提供的占位符保持原样（方便链式渲染）。
    /// </summary>
    public String Render(IDictionary<String, Object?> variables)
    {
        if (variables == null) throw new ArgumentNullException(nameof(variables));

        return PlaceholderRegex.Replace(_template, match =>
        {
            var name = match.Groups["name"].Value;
            return variables.TryGetValue(name, out var value)
                ? value?.ToString() ?? ""
                : match.Value;
        });
    }

    /// <summary>用匿名对象的属性作为变量渲染模板。</summary>
    public String Render(Object variables)
    {
        if (variables == null) throw new ArgumentNullException(nameof(variables));

        var dict = new Dictionary<String, Object?>();
        foreach (var prop in variables.GetType().GetProperties())
            dict[prop.Name] = prop.GetValue(variables);
        return Render(dict);
    }

    public override String ToString() => _template;

    public static implicit operator PromptTemplate(String template) => new(template);
}
