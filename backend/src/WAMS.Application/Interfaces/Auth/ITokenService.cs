namespace WAMS.Application.Interfaces.Auth;

using WAMS.Domain.Entities.Users;

public interface ITokenService
{
    string GenerateAccessToken(User user, List<string> roles, long companyId, bool hasWildcard = false);
    string GenerateRefreshToken();
    Task BlacklistTokenAsync(string jti, TimeSpan expiry);
    Task<bool> IsTokenBlacklistedAsync(string jti);
}
