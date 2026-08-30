namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Entities.TransportOrders;

public class WorkOrderTransportOrder
{
    public long WorkOrderId { get; set; }
    public long TransportOrderShadowId { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
    public TransportOrderShadow TransportOrderShadow { get; set; } = null!;
}
