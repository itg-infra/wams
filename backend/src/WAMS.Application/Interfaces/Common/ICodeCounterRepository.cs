namespace WAMS.Application.Interfaces.Common;

/// <summary>
/// Atomic sequence counter keyed by code prefix (e.g. "BP-2605").
/// Safe under concurrent writers - the UPSERT runs in a single statement.
/// </summary>
public interface ICodeCounterRepository
{
    Task<long> NextValueAsync(string prefix, CancellationToken ct = default);
    Task<long> NextRangeAsync(string prefix, int count, CancellationToken ct = default);
}
