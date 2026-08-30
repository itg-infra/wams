namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using WAMS.Application.Services.AccountPayables;
using WAMS.Domain.Entities.AccountPayables;
using Xunit;

public class AccountPayableTotalsCalculatorTests
{
    private static AccountPayableItem MakeItem(
        decimal budgetPlanTotal, decimal ppnAmount, decimal pphAmount,
        decimal grandTotal, decimal budgetRealization, decimal budgetVariance) => new()
    {
        BudgetPlanTotal = budgetPlanTotal,
        PpnAmount = ppnAmount,
        PphAmount = pphAmount,
        GrandTotal = grandTotal,
        BudgetRealization = budgetRealization,
        BudgetVariance = budgetVariance,
    };

    [Fact]
    public void Compute_ZeroDiscount_DiscountPercentIsZeroAndTotalsUnaffected()
    {
        var items = new[]
        {
            MakeItem(100m, 11m, 2m, 113m, 90m, 10m),
            MakeItem(200m, 22m, 4m, 226m, 180m, 20m),
        };

        var result = AccountPayableTotalsCalculator.Compute(items, 0m);

        result.DppTotal.Should().Be(300m);
        result.TotalPpnAmount.Should().Be(33m);
        result.TotalPphAmount.Should().Be(6m);
        result.TaxInclusiveGrandTotal.Should().Be(339m);
        result.DiscountAmount.Should().Be(0m);
        result.DiscountPercent.Should().Be(0m);
        result.TotalRealization.Should().Be(270m);
        result.TotalVariance.Should().Be(30m);
    }

    [Fact]
    public void Compute_WithDiscount_NetsOutOfGrandTotalAndVarianceAndComputesPercent()
    {
        var items = new[]
        {
            MakeItem(400m, 44m, 8m, 452m, 400m, 0m),
        };

        var result = AccountPayableTotalsCalculator.Compute(items, 40m);

        result.DppTotal.Should().Be(400m);
        result.TaxInclusiveGrandTotal.Should().Be(412m); // 452 - 40
        result.TotalVariance.Should().Be(-40m);           // 0 - 40
        result.DiscountAmount.Should().Be(40m);
        result.DiscountPercent.Should().Be(10m);          // 40 / 400 * 100
    }

    [Fact]
    public void Compute_EmptyItems_NoDivideByZero()
    {
        var result = AccountPayableTotalsCalculator.Compute([], 50m);

        result.DppTotal.Should().Be(0m);
        result.DiscountPercent.Should().Be(0m);
        result.TaxInclusiveGrandTotal.Should().Be(-50m);
        result.TotalVariance.Should().Be(-50m);
    }
}
