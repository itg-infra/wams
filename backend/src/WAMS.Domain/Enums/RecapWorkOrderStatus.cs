namespace WAMS.Domain.Enums;

using Ardalis.SmartEnum;

public sealed class RecapWorkOrderStatus : SmartEnum<RecapWorkOrderStatus, string>
{
    public static readonly RecapWorkOrderStatus Pending = new(nameof(Pending), "Pending");
    public static readonly RecapWorkOrderStatus Approved = new(nameof(Approved), "Approved");
    public static readonly RecapWorkOrderStatus Rejected = new(nameof(Rejected), "Rejected");

    private RecapWorkOrderStatus(string name, string value) : base(name, value) { }

    public bool CanBeReviewed => this == Pending;
}
