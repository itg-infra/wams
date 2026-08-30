namespace WAMS.Application.Services.AccountPayables;

using WAMS.Domain.Entities.AccountPayables;

public record AccountPayableTotals(
    decimal DppTotal,
    decimal TotalPpnAmount,
    decimal TotalPphAmount,
    decimal TaxInclusiveGrandTotal,
    decimal DiscountAmount,
    decimal DiscountPercent,
    decimal TotalRealization,
    decimal TotalVariance
);

public static class AccountPayableTotalsCalculator
{
    public static AccountPayableTotals Compute(IEnumerable<AccountPayableItem> items, decimal discountAmount)
    {
        var list = items as ICollection<AccountPayableItem> ?? [.. items];

        var dppTotal = list.Sum(i => i.BudgetPlanTotal);
        var totalPpnAmount = list.Sum(i => i.PpnAmount);
        var totalPphAmount = list.Sum(i => i.PphAmount);
        var taxInclusiveGrandTotal = list.Sum(i => i.GrandTotal) - discountAmount;
        var totalRealization = list.Sum(i => i.BudgetRealization);
        var totalVariance = list.Sum(i => i.BudgetVariance) - discountAmount;
        var discountPercent = dppTotal == 0m ? 0m : discountAmount / dppTotal * 100m;

        return new AccountPayableTotals(
            dppTotal,
            totalPpnAmount,
            totalPphAmount,
            taxInclusiveGrandTotal,
            discountAmount,
            discountPercent,
            totalRealization,
            totalVariance
        );
    }
}
