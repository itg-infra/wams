namespace WAMS.Infrastructure.ExternalSync.Common;

public record SyncResult(
    string ServiceName,
    int Added,
    int Updated,
    int Deactivated,
    int Skipped,
    bool Success,
    string? ErrorMessage = null
)
{
    public static SyncResult Failed(string serviceName, string errorMessage) =>
        new(serviceName, 0, 0, 0, 0, false, errorMessage);
}