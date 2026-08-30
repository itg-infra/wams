namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;

public class WorkOrderUnbaggingDetail : BaseEntity
{
    public long WorkOrderId { get; set; }
    public string? NoVehicle { get; set; }
    public string? NoContainer { get; set; }
    public string? NoSeal { get; set; }
    public decimal? InitialWeight { get; set; }
    public decimal? FinalWeight { get; set; }
    public decimal? UnitWeight { get; set; }
    public decimal? TotalWeight { get; set; }
    public int? TotalBag { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}
