namespace WAMS.Application.DTOs.RecapWorkOrders;

using System.ComponentModel.DataAnnotations;

public record RecapWorkOrderQuery(
    string? Status = null,
    string? Search = null,
    string? SortBy = null,
    string? SortOrder = null,
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int Limit = 20);
