namespace WAMS.Application.Common;

using System.ComponentModel.DataAnnotations;

public record DataTableQuery
{
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string SortOrder { get; init; } = "asc";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [Range(1, 100)] public int Limit { get; init; } = 20;
}
