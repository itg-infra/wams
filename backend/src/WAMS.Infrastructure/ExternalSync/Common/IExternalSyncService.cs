namespace WAMS.Infrastructure.ExternalSync.Common;

public interface IExternalSyncService
{
    string ServiceName { get; }

    Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default);
}
