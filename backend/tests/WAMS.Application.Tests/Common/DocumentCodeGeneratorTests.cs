namespace WAMS.Application.Tests.Common;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Common;
using WAMS.Application.Interfaces.Common;
using Xunit;

public class DocumentCodeGeneratorTests
{
    private readonly ICodeCounterRepository _counterRepo = Substitute.For<ICodeCounterRepository>();

    [Fact]
    public async Task NextCodeAsync_FormatsPrefixAndSequenceWithSixDigits()
    {
        _counterRepo.NextValueAsync("PO-2607", Arg.Any<CancellationToken>()).Returns(42L);

        var code = await DocumentCodeGenerator.NextCodeAsync(_counterRepo, "PO-2607", TestContext.Current.CancellationToken);

        code.Should().Be("PO-2607000042");
    }

    [Fact]
    public async Task NextCodeAsync_PadsLargeSequenceToSixDigitsWithoutTruncation()
    {
        _counterRepo.NextValueAsync("BT-2607", Arg.Any<CancellationToken>()).Returns(999999L);

        var code = await DocumentCodeGenerator.NextCodeAsync(_counterRepo, "BT-2607", TestContext.Current.CancellationToken);

        code.Should().Be("BT-2607999999");
    }
}
