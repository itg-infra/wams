namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class BudgetPlanType : SmartEnum<BudgetPlanType, string>
{
    public static readonly BudgetPlanType External = new(nameof(External), "External");
    public static readonly BudgetPlanType Internal = new(nameof(Internal), "Internal");

    private BudgetPlanType(string name, string value) : base(name, value) { }
}
