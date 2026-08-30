namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;

public class WorkOrderStorageDetail : BaseEntity
{
    public long WorkOrderId { get; set; }
    public bool HasPindahStapel { get; set; }
    public bool HasPembersihan { get; set; }
    public bool HasPerapihan { get; set; }
    public decimal? VolumeWeight { get; set; }
    public int? WorkerOnDuty { get; set; }
    public bool HasMask { get; set; }
    public bool HasSafetyGlasses { get; set; }
    public bool HasHandGloves { get; set; }
    public bool HasHelmet { get; set; }
    public bool HasSafetyShoes { get; set; }
    public bool HasSafetyVest { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}
