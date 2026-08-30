namespace WAMS.Application.DTOs.SyncLogs;

using WAMS.Application.Common;
using WAMS.Domain.Entities.SyncLogs;

public record SyncLogQuery : DataTableQuery
{
    public string? ServiceName { get; init; }
    public string? CompanyCode { get; init; }
    public SyncOutcome? Outcome { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

public record SyncLogResponse(
    long Id,
    string ServiceName,
    string CompanyCode,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Outcome,
    int Added,
    int Updated,
    int Deactivated,
    string? AbortReason,
    double? DurationMs
);

public record SyncLogLatestResponse(
    string ServiceName,
    string CompanyCode,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string Outcome,
    int Added,
    int Updated,
    int Deactivated,
    string? AbortReason,
    double? DurationMs
);
