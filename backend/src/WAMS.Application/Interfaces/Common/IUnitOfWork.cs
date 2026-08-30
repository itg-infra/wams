namespace WAMS.Application.Interfaces.Common;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
