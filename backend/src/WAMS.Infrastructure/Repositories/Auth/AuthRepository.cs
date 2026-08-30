namespace WAMS.Infrastructure.Repositories.Auth;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Auth;
using WAMS.Domain.Entities.Auth;
using WAMS.Infrastructure.Data;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _db;

    public AuthRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Add(token);
        return Task.FromResult(token);
    }

    public async Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task RevokeRefreshTokenAsync(long tokenId, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(rt => rt.Id == tokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeAllUserTokensAsync(long userId, long? exceptTokenId = null, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.Id != exceptTokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAt, DateTime.UtcNow), ct);
    }
}
