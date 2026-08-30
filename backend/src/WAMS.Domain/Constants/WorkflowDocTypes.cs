namespace WAMS.Domain.Constants;

public static class WorkflowDocTypes
{
    public const string BudgetPlanApproval = "BudgetPlanApproval";

    public static readonly IReadOnlyList<(string Value, string Label)> All =
    [
        (BudgetPlanApproval, "Budget Plan Approval"),
    ];

    public static readonly IReadOnlySet<string> ValidValues =
        All.Select(x => x.Value).ToHashSet();
}
