namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class BudgetTemplateStatus : SmartEnum<BudgetTemplateStatus, string>
{
    public static readonly BudgetTemplateStatus Draft = new(nameof(Draft), "Draft");
    public static readonly BudgetTemplateStatus Submitted = new(nameof(Submitted), "Submitted");

    private BudgetTemplateStatus(string name, string value) : base(name, value) { }

    public bool CanBeEdited => this == Draft || this == Submitted;
    public bool CanBeDeleted => this == Draft;
    public bool CanBeSubmitted => this == Draft;
}
