namespace WAMS.Infrastructure.Services.AuditLogs;

using System.Threading.Channels;
using WAMS.Domain.Entities.AuditLogs;

public interface IAuditLogQueue
{
    void Enqueue(IReadOnlyList<AuditLog> logs);
    IAsyncEnumerable<IReadOnlyList<AuditLog>> ReadBatchesAsync(CancellationToken ct);
}

public sealed class AuditLogQueue : IAuditLogQueue
{
    private readonly Channel<IReadOnlyList<AuditLog>> _channel =
        Channel.CreateUnbounded<IReadOnlyList<AuditLog>>(new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(IReadOnlyList<AuditLog> logs)
        => _channel.Writer.TryWrite(logs);

    public IAsyncEnumerable<IReadOnlyList<AuditLog>> ReadBatchesAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
