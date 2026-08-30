namespace WAMS.Application.Tests.Common;

using FluentAssertions;
using WAMS.Application.Common;
using Xunit;

public class TaxCalculatorTests
{
    [Fact]
    public void Calculate_NoTax_ReturnsZeroAmountsAndGrandTotalEqualsBase()
    {
        var result = TaxCalculator.Calculate(totalValue: 1000m, ppnRate: 0m, pphRate: 0m);

        result.PpnAmount.Should().Be(0m);
        result.PphAmount.Should().Be(0m);
        result.GrandTotal.Should().Be(1000m);
    }

    [Fact]
    public void Calculate_PpnOnly_AddsPpnAmountToGrandTotal()
    {
        var result = TaxCalculator.Calculate(totalValue: 1000m, ppnRate: 11m, pphRate: 0m);

        result.PpnAmount.Should().Be(110.00m);
        result.PphAmount.Should().Be(0m);
        result.GrandTotal.Should().Be(1110.00m);
    }

    [Fact]
    public void Calculate_PphOnly_SubtractsPphAmountFromGrandTotal()
    {
        var result = TaxCalculator.Calculate(totalValue: 1000m, ppnRate: 0m, pphRate: 2m);

        result.PpnAmount.Should().Be(0m);
        result.PphAmount.Should().Be(20.00m);
        result.GrandTotal.Should().Be(980.00m);
    }

    [Fact]
    public void Calculate_BothPpnAndPph_CombinesAdditiveAndDeductive()
    {
        var result = TaxCalculator.Calculate(totalValue: 1000m, ppnRate: 11m, pphRate: 2m);

        result.PpnAmount.Should().Be(110.00m);
        result.PphAmount.Should().Be(20.00m);
        result.GrandTotal.Should().Be(1090.00m);
    }
}
