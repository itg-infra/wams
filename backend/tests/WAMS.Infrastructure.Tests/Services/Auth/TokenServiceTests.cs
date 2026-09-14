using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using WAMS.Domain.Entities.Users;
using Xunit;
using WAMS.Infrastructure.Services.Auth;

namespace WAMS.Infrastructure.Tests.Services.Auth;

public sealed class TokenServiceTests
{
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-that-is-long-enough-for-hmac-sha256",
            ["Jwt:ExpirationMinutes"] = "15"
        })
        .Build();

    [Fact]
    public async Task BlacklistTokenAsync_MakesJtiBlacklisted()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TokenService(_configuration, cache);

        await sut.BlacklistTokenAsync("jti-1", TimeSpan.FromMinutes(1));

        (await sut.IsTokenBlacklistedAsync("jti-1")).Should().BeTrue();
    }

    [Fact]
    public async Task IsTokenBlacklistedAsync_ReturnsFalseForUnknownJti()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TokenService(_configuration, cache);

        (await sut.IsTokenBlacklistedAsync("missing")).Should().BeFalse();
    }

    [Fact]
    public async Task BlacklistTokenAsync_UsesExpiration()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TokenService(_configuration, cache);

        await sut.BlacklistTokenAsync("jti-expiring", TimeSpan.Zero);

        (await sut.IsTokenBlacklistedAsync("jti-expiring")).Should().BeFalse();
    }

    [Fact]
    public void GenerateAccessToken_ForMembership_IncludesCompanyAndMembershipClaims()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TokenService(_configuration, cache);
        var token = sut.GenerateAccessToken(
            new User { Id = 7, Email = "user@example.com", Fullname = "User", SessionVersion = 2 },
            new[] { "VIEWER" },
            companyId: 3,
            userCompanyId: 9,
            membershipAuthorizationVersion: 4);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "company_id" && c.Value == "3");
        jwt.Claims.Should().Contain(c => c.Type == "user_company_id" && c.Value == "9");
        jwt.Claims.Should().Contain(c =>
            c.Type == "membership_authorization_version" && c.Value == "4");
    }

    [Fact]
    public void GenerateAccessToken_ForSystemSession_OmitsMembershipClaims()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new TokenService(_configuration, cache);
        var token = sut.GenerateAccessToken(
            new User { Id = 7, Email = "admin@example.com", Fullname = "Admin" },
            new[] { "SUPER_ADMIN" },
            companyId: 3,
            userCompanyId: null,
            membershipAuthorizationVersion: null,
            hasWildcard: true);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == "company_id" && c.Value == "3");
        jwt.Claims.Should().NotContain(c => c.Type == "user_company_id");
        jwt.Claims.Should().NotContain(c => c.Type == "membership_authorization_version");
    }
}
