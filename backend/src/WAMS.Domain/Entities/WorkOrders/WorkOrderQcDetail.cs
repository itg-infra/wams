namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;

public class WorkOrderQcDetail : BaseEntity
{
    public long WorkOrderId { get; set; }
    public decimal? MoisturePercent { get; set; }
    public decimal? JamurPercent { get; set; }
    public decimal? BauPercent { get; set; }
    public string? QualityStatus { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}
