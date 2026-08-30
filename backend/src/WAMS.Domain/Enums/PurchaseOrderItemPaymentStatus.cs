namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class PurchaseOrderItemPaymentStatus : SmartEnum<PurchaseOrderItemPaymentStatus, string>
{
    public static readonly PurchaseOrderItemPaymentStatus Unpaid = new(nameof(Unpaid), "Unpaid");
    public static readonly PurchaseOrderItemPaymentStatus Paid = new(nameof(Paid), "Paid");

    private PurchaseOrderItemPaymentStatus(string name, string value) : base(name, value) { }
}
