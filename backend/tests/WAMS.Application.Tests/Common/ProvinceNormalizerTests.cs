// tests/WAMS.Application.Tests/Common/ProvinceNormalizerTests.cs
using FluentAssertions;
using WAMS.Application.Common;
using Xunit;

namespace WAMS.Application.Tests.Common;

public class ProvinceNormalizerTests
{
    [Theory]
    [InlineData("Lampung", "LAMPUNG")]
    [InlineData("  Jawa Timur ", "JAWA TIMUR")]
    [InlineData("Nusa tenggara Barat", "NUSA TENGGARA BARAT")]
    [InlineData("Kepulauan  Bangka   Belitung", "KEPULAUAN BANGKA BELITUNG")]
    public void Normalize_uppercases_trims_and_collapses_whitespace(string raw, string expected)
        => ProvinceNormalizer.Normalize(raw).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_blank_returns_empty(string? raw)
        => ProvinceNormalizer.Normalize(raw).Should().Be("");
}
