namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class BudgetPlanStatus : SmartEnum<BudgetPlanStatus, string>
{
    public static readonly BudgetPlanStatus Draft = new(nameof(Draft), "Draft", "Draft");
    public static readonly BudgetPlanStatus Submitted = new(nameof(Submitted), "Submitted", "Submitted");
    public static readonly BudgetPlanStatus InApproval = new(nameof(InApproval), "InApproval", "In Approval");
    public static readonly BudgetPlanStatus Approved = new(nameof(Approved), "Approved", "Approved");
    public static readonly BudgetPlanStatus Rejected = new(nameof(Rejected), "Rejected", "Rejected");

    public string DisplayName { get; }

    private BudgetPlanStatus(string name, string value, string displayName) : base(name, value)
        => DisplayName = displayName;

    public bool CanBeEdited => this == Draft || this == Rejected;
    public bool CanBeDeleted => this == Draft;
    public bool CanBeSubmitted => this == Draft || this == Rejected;
    public bool CanBeApproved => this == Submitted || this == InApproval;
    public bool CanBeRejected => this == Submitted || this == InApproval;
}
