namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;

public class WorkOrderHeavyEquipDetail : BaseEntity
{
    public long WorkOrderId { get; set; }
    public string? BlNumber { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? StandbyDuration1 { get; set; }
    public string? StandbyDuration2 { get; set; }
    public string? MinimumDuration { get; set; }
    public decimal? CostPerHour { get; set; }
    public decimal? TotalCost { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}
