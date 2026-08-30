namespace WAMS.Domain.Entities.BudgetPlans;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;

public class BudgetPlanItem : BaseEntity
{
    public long BudgetPlanId { get; set; }
    public long ItemShadowId { get; set; }
    public long ActivityTypeId { get; set; }
    public long VendorShadowId { get; set; }
    public long UomMasterId { get; set; }
    public BudgetPlanType Type { get; set; } = BudgetPlanType.External;
    public bool IsRfba { get; set; } = false;
    public decimal CostValue { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalValue { get; set; }
    public string? PpnTaxTypeCode { get; set; }
    public decimal PpnRate { get; set; }
    public string? PphTaxTypeCode { get; set; }
    public decimal PphRate { get; set; }
    public string? CostTreatment { get; set; }
    public decimal PpnAmount { get; set; }
    public decimal PphAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public int SortOrder { get; set; }
    public string? DocExternal { get; set; }
    public string? BillOfLading { get; set; }
    public string? Description { get; set; }
    public long? SpkShadowId { get; set; }

    public BudgetPlan BudgetPlan { get; set; } = null!;
    public ItemShadow Item { get; set; } = null!;
    public SpkShadow? Spk { get; set; }
    public ActivityType? ActivityType { get; set; }
    public VendorShadow Vendor { get; set; } = null!;
    public UomMaster Uom { get; set; } = null!;
}
