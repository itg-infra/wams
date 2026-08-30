namespace WAMS.Domain.Entities.PurchaseOrders;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;

public class PurchaseOrderItem : BaseEntity
{
    public long PurchaseOrderId { get; set; }
    public long BudgetPlanItemId { get; set; }

    // Snapshot fields - copied at creation, frozen after Generate
    public long ItemShadowId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CoaCode { get; set; } = string.Empty;
    public string CoaName { get; set; } = string.Empty;
    public long VendorShadowId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public long UomMasterId { get; set; }
    public string UomCode { get; set; } = string.Empty;
    public string UomName { get; set; } = string.Empty;
    public bool IsRfba { get; set; }
    public string? BillOfLading { get; set; }
    public decimal CostValue { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public int SortOrder { get; set; }
    public string? PpnTaxTypeCode { get; set; }
    public decimal PpnRate { get; set; }
    public string? PphTaxTypeCode { get; set; }
    public decimal PphRate { get; set; }
    public string? CostTreatment { get; set; }
    public decimal PpnAmount { get; set; }
    public decimal PphAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public PurchaseOrderItemPaymentStatus PaymentStatus { get; set; } = PurchaseOrderItemPaymentStatus.Unpaid;

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public BudgetPlanItem BudgetPlanItem { get; set; } = null!;
}
