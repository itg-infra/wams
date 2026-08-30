namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Spk;

public class WorkOrderLoadingItem : BaseEntity
{
    public long WorkOrderId { get; set; }
    public long? SpkShadowId { get; set; }
    public string BlNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public string? NoVehicle { get; set; }
    public string? NoContainer { get; set; }
    public string? NoSeal { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? FinalWeight { get; set; }
    public decimal? NettWeight { get; set; }
    public int? TotalBag { get; set; }
    public decimal? UnitWeight { get; set; }
    public bool IsChecked { get; set; }
    public int SortOrder { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
    public SpkShadow? SpkShadow { get; set; }
}
