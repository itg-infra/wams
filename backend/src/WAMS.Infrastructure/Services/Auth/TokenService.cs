namespace WAMS.Infrastructure.Services.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WAMS.Application.Interfaces.Auth;
using WAMS.Domain.Entities.Users;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly IMemoryCache _memoryCache;

    public TokenService(IConfiguration config, IMemoryCache memoryCache)
    {
        _config = config;
        _memoryCache = memoryCache;
    }

    public string GenerateAccessToken(User user, List<string> roles, long companyId, bool hasWildcard = false)
    {
        var secret = _config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullname", user.Fullname),
            new("company_id", companyId.ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (hasWildcard)
            claims.Add(new Claim("permissions", "*.*.*"));

        var expirationMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "1440");

        var token = new JwtSecurityToken(
            issuer: "WAMS",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public Task BlacklistTokenAsync(string jti, TimeSpan expiry)
    {
        // Revocation is process-local because the distributed cache is not configured.
        var key = $"blacklist:{jti}";
        if (expiry <= TimeSpan.Zero)
            _memoryCache.Remove(key);
        else
            _memoryCache.Set(key, true, expiry);

        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string jti)
        => Task.FromResult(_memoryCache.TryGetValue($"blacklist:{jti}", out _));
}
