using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WAMS.Domain.Entities.Auth;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.Auth;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

public class AuthRepositoryTests
{
    // SQLite in-memory DBs are destroyed when the last connection to them closes,
    // so the connection returned here must be kept open for the test's lifetime.
    // EF Core's InMemory provider does not support ExecuteUpdateAsync (used by
    // AuthRepository.RevokeAllUserTokensAsync), so this test uses SQLite instead.
    private static (DbContextOptions<AppDbContext> options, SqliteConnection connection) NewDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using (var db = new AppDbContext(options, NSubstitute.Substitute.For<WAMS.Application.Interfaces.Common.ITenantContext>()))
        {
            db.Database.EnsureCreated();
        }
        return (options, connection);
    }

    private static AppDbContext Open(DbContextOptions<AppDbContext> o)
        => new AppDbContext(o, NSubstitute.Substitute.For<WAMS.Application.Interfaces.Common.ITenantContext>());

    private static async Task<(long tokenAId, long tokenBId)> SeedTwoActiveTokensAsync(
        DbContextOptions<AppDbContext> o, long userId)
    {
        await using var db = Open(o);
        db.Companies.Add(new Company { Id = 1, Name = "C", Code = "C001", IsActive = true });
        db.Users.Add(new User { Id = userId, Email = "u@t.c", Fullname = "U", CompanyId = 1, IsActive = true });
        var a = new RefreshToken { UserId = userId, TokenHash = "hash-a", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        var b = new RefreshToken { UserId = userId, TokenHash = "hash-b", ExpiresAt = DateTime.UtcNow.AddDays(7) };
        db.RefreshTokens.AddRange(a, b);
        await db.SaveChangesAsync();
        return (a.Id, b.Id);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithExceptTokenId_RevokesOnlyOtherTokens()
    {
        var (opts, connection) = NewDb();
        using (connection)
        {
            var (tokenAId, tokenBId) = await SeedTwoActiveTokensAsync(opts, userId: 1);

            await using (var db = Open(opts))
            {
                var repo = new AuthRepository(db);
                await repo.RevokeAllUserTokensAsync(userId: 1, exceptTokenId: tokenAId, ct: TestContext.Current.CancellationToken);
            }

            await using var verify = Open(opts);
            var tokenA = await verify.RefreshTokens.SingleAsync(t => t.Id == tokenAId, cancellationToken: TestContext.Current.CancellationToken);
            var tokenB = await verify.RefreshTokens.SingleAsync(t => t.Id == tokenBId, cancellationToken: TestContext.Current.CancellationToken);
            tokenA.RevokedAt.Should().BeNull();
            tokenB.RevokedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithoutExceptTokenId_RevokesAllTokens()
    {
        var (opts, connection) = NewDb();
        using (connection)
        {
            var (tokenAId, tokenBId) = await SeedTwoActiveTokensAsync(opts, userId: 1);

            await using (var db = Open(opts))
            {
                var repo = new AuthRepository(db);
                await repo.RevokeAllUserTokensAsync(userId: 1, ct: TestContext.Current.CancellationToken);
            }

            await using var verify = Open(opts);
            var tokenA = await verify.RefreshTokens.SingleAsync(t => t.Id == tokenAId, cancellationToken: TestContext.Current.CancellationToken);
            var tokenB = await verify.RefreshTokens.SingleAsync(t => t.Id == tokenBId, cancellationToken: TestContext.Current.CancellationToken);
            tokenA.RevokedAt.Should().NotBeNull();
            tokenB.RevokedAt.Should().NotBeNull();
        }
    }
}
