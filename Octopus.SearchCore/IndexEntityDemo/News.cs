namespace Octopus.SearchCore.IndexDemo;

public class News : LuceneIndexableBaseEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [LuceneIndex]
    public string Title { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [LuceneIndex]
    public string Author { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [LuceneIndex(IsHtml = true)]
    public string Content { get; set; }

    /// <summary>
    /// 发表时间
    /// </summary>
    public DateTime PostDate { get; set; }

}