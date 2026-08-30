namespace WAMS.Domain.Entities.SyncLogs;

public class SyncLog
{
    public long Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public SyncOutcome Outcome { get; set; }
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Deactivated { get; set; }
    public string? AbortReason { get; set; }
}
