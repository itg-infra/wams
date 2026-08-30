namespace WAMS.Domain.Entities.AuditLogs;

public class AuditLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullname { get; set; }
    public long? CompanyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long? RecordId { get; set; }
    public string? RecordKey { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? RequestId { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
