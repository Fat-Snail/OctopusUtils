namespace OctopusEx.WebCore.DomainCore.APICommon;

using System.Text.Json.Serialization;

/// <summary>
/// 分页数据接口，供实现类和调用方统一访问分页信息
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public interface IPagedData<T>
{
    int Page { get; set; }
    int PageSize { get; set; }
    long TotalCount { get; set; }
    int TotalPages { get; set; }
    List<T> Items { get; set; }
    bool IsFirstPage { get; }
    bool IsLastPage { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
}

/// <summary>
/// 分页数据实现，基础实现 IPagedData
/// </summary>
public class PagedData<T> : IPagedData<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = new List<T>();

    [JsonPropertyName("isFirstPage")]
    public bool IsFirstPage => Page == 1;
    [JsonPropertyName("isLastPage")]
    public bool IsLastPage => Page >= TotalPages;
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage => Page > 1;
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage => Page < TotalPages;
}
