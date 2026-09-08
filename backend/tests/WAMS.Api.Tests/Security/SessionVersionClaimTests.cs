using FluentAssertions;
using Xunit;
using WAMS.Api.Security;

namespace WAMS.Api.Tests.Security;

public class SessionVersionClaimTests
{
    [Fact]
    public void TryParse_MissingClaim_UsesInitialSessionVersion()
    {
        SessionVersionClaim.TryParse(null, out var version).Should().BeTrue();
        version.Should().Be(0);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("3", 3)]
    public void TryParse_ValidClaim_UsesClaimVersion(string claim, int expected)
    {
        SessionVersionClaim.TryParse(claim, out var version).Should().BeTrue();
        version.Should().Be(expected);
    }

    [Fact]
    public void TryParse_InvalidClaim_IsRejected()
    {
        SessionVersionClaim.TryParse("not-a-version", out _).Should().BeFalse();
    }
}
