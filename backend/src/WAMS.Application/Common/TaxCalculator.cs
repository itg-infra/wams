namespace WAMS.Application.Common;

public static class TaxCalculator
{
    public record Result(decimal PpnAmount, decimal PphAmount, decimal GrandTotal);

    public static Result Calculate(decimal totalValue, decimal ppnRate, decimal pphRate)
    {
        var ppnAmount = Math.Round(totalValue * ppnRate / 100m, 2, MidpointRounding.AwayFromZero);
        var pphAmount = Math.Round(totalValue * pphRate / 100m, 2, MidpointRounding.AwayFromZero);
        var grandTotal = totalValue + ppnAmount - pphAmount;

        return new Result(ppnAmount, pphAmount, grandTotal);
    }
}
