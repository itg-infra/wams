namespace WAMS.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<int> CommitAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(ErrorMessages.BudgetPlan.AlreadyProcessed);
        }
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await operation(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
}
