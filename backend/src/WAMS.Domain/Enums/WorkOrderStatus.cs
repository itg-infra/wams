namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class WorkOrderStatus : SmartEnum<WorkOrderStatus, string>
{
    public static readonly WorkOrderStatus Draft = new(nameof(Draft), "Draft");
    public static readonly WorkOrderStatus Submitted = new(nameof(Submitted), "Submitted");

    private WorkOrderStatus(string name, string value) : base(name, value) { }

    public bool CanBeEdited => this == Draft;
    public bool CanBeDeleted => this == Draft;
    public bool CanBeSubmitted => this == Draft;
}
