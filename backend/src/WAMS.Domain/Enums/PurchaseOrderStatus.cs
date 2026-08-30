namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class PurchaseOrderStatus : SmartEnum<PurchaseOrderStatus, string>
{
    public static readonly PurchaseOrderStatus Draft = new(nameof(Draft), "Draft");
    public static readonly PurchaseOrderStatus Generated = new(nameof(Generated), "Generated");

    private PurchaseOrderStatus(string name, string value) : base(name, value) { }

    public bool CanBeEdited => this == Draft;
    public bool CanBeDeleted => this == Draft;
    public bool CanBeGenerated => this == Draft;
}
