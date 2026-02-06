namespace OctopusEx.WebCore.DomainCore.APICommon;

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class PageRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [Range(1, 1000, ErrorMessage = "每页记录数必须在1-1000之间")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;

    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; }

    [RegularExpression("^(asc|desc)$", ErrorMessage = "排序方向必须是asc或desc")]
    [JsonPropertyName("sortDirection")]
    public string SortDirection { get; set; } = "desc";

    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    public int GetSkipCount()
    {
        return (Page - 1) * PageSize;
    }

    public int GetValidPageSize()
    {
        return Math.Clamp(PageSize, 1, 1000);
    }

    public int GetValidPage()
    {
        return Math.Max(Page, 1);
    }
}
