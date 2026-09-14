namespace WAMS.Infrastructure.Tests.Data;

using Microsoft.EntityFrameworkCore;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.Data;
using Xunit;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_UsesResourceNeutralMessageForConcurrencyConflicts()
    {
        await using var db = new ConcurrencyFailingDbContext();
        var unitOfWork = new UnitOfWork(db);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => unitOfWork.CommitAsync(TestContext.Current.CancellationToken));

        Assert.Equal("The resource was modified by another request. Please reload and try again.", exception.Message);
    }

    private sealed class ConcurrencyFailingDbContext()
        : AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw new DbUpdateConcurrencyException("simulated concurrency conflict");
    }
}
