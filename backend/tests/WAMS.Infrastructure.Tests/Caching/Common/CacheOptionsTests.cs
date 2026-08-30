using FluentAssertions;
using WAMS.Infrastructure.Caching.Common;
using Xunit;

namespace WAMS.Infrastructure.Tests.Caching.Common;

public sealed class CacheOptionsTests
{
    [Fact]
    public void ToHybridOptions_UsesTheLocalTtlForBothExpirations()
    {
        var config = new CacheEntryConfig(42);

        var options = config.ToHybridOptions();

        options.LocalCacheExpiration.Should().Be(TimeSpan.FromSeconds(42));
        options.Expiration.Should().Be(TimeSpan.FromSeconds(42));
    }
}
