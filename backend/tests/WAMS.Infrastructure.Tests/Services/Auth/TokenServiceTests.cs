using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
}
