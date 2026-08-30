namespace WAMS.Application.Interfaces.Auth;

using WAMS.Domain.Entities.Auth;

public interface IAuthRepository
{
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(long tokenId, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(long userId, long? exceptTokenId = null, CancellationToken ct = default);
}
