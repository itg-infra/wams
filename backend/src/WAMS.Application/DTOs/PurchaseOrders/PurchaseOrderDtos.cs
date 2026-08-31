namespace WAMS.Application.DTOs.PurchaseOrders;

using WAMS.Application.Common;

public record PoLinkInfo(long Id, string Code);

// One entry per stage of the source budget plan's approval workflow, in StageOrder,
// so the printed "Disetujui Oleh" block scales with the company's workflow template -
// same shape as RcaSignatures.Approvers. Name/date are null for an unapproved stage.
public record PoApprover(string? Name, DateTime? ApprovedAt);

public record BpLinkInfo(long Id, string Code, List<PoLinkInfo> PurchaseOrders);

public record PurchaseOrderQuery : DataTableQuery
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public long? VendorShadowId { get; init; }
    public string? Status { get; init; }
}

public record AvailablePoItemQuery : DataTableQuery
{
    public long BudgetPlanId { get; init; }
    public long? VendorShadowId { get; init; }
    public bool IncludeGenerated { get; init; }
}

public record EditAvailablePoItemQuery : DataTableQuery
{
    public bool IncludeGenerated { get; init; }
}

public record CreatePurchaseOrderRequest(
    long VendorShadowId,
    string? Remark,
    DateTime DocDate,
    List<long> Items);

public record InvalidPurchaseOrderItem(
    long ItemId,
    long RequestedVendorShadowId,
    long? ActualVendorShadowId);

public record PurchaseOrderItemValidationDetails(
    List<InvalidPurchaseOrderItem> InvalidItems);

public record UpdatePurchaseOrderRequest(
    string? Remark,
    DateTime? DocDate,
    List<long>? Items);

public record PurchaseOrderSummaryResponse(
    long Id,
    string Code,
    string VendorCode,
    string VendorName,
    string Status,
    DateTime DocDate,
    string? Remark,
    string? SapPoNumber,
    decimal GrandTotal,
    int ItemCount,
    DateTime CreatedAt,
    string CreatedByName);

public record PurchaseOrderResponse(
    long Id,
    string Code,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    string Status,
    DateTime DocDate,
    string? Remark,
    string? SapPoNumber,
    List<BpLinkInfo> LinkedBudgetPlans,
    List<PurchaseOrderItemResponse> Items,
    decimal GrandTotal,
    decimal TotalPpnAmount,
    decimal TotalPphAmount,
    decimal TaxInclusiveGrandTotal,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? GeneratedAt,
    string? GeneratedByName,
    IReadOnlyList<PoApprover> Approvers,
    PurchaseOrderApdpResponse? Apdp = null);

public record PurchaseOrderApdpResponse(
    string Status,
    int? SapDocEntry,
    decimal Amount,
    DateTime? GeneratedAt,
    string? Error);

public record PurchaseOrderItemResponse(
    long Id,
    long BudgetPlanItemId,
    long ItemShadowId,
    string ItemCode,
    string ItemName,
    string CoaCode,
    string CoaName,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    long UomMasterId,
    string UomCode,
    string UomName,
    bool IsRfba,
    string? BillOfLading,
    decimal CostValue,
    decimal Quantity,
    decimal TotalValue,
    int SortOrder,
    string? PpnTaxTypeCode,
    decimal PpnRate,
    string? PphTaxTypeCode,
    decimal PphRate,
    decimal PpnAmount,
    decimal PphAmount,
    decimal GrandTotal,
    string? CostTreatment);

public record BudgetPlanItemAvailability(
    long Id,
    bool Found,
    bool VendorMatches,
    bool WarehouseInScope,
    bool PlanApproved,
    bool AlreadyGenerated,
    string? TakenByCode,
    long? ActualVendorShadowId = null);

/// <summary>
/// One source-aware picker row. Each row includes its originating budget plan and warehouse
/// metadata so the client can distinguish same-vendor items across accessible warehouses.
/// </summary>
public record AvailablePoItemResponse(
    long BudgetPlanItemId,
    long BudgetPlanId,
    string BudgetPlanCode,
    string? BudgetPlanRemark,
    DateTime BudgetPlanDocDate,
    bool IsSeedBudgetPlan,
    long WarehouseShadowId,
    string WarehouseCode,
    string WarehouseName,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    long ItemShadowId,
    string ItemCode,
    string ItemName,
    string CoaCode,
    string CoaName,
    bool IsRfba,
    string? BillOfLading,
    decimal CostValue,
    decimal Quantity,
    string UomCode,
    string UomName,
    bool IsGenerated,
    string? TakenByCode,
    string AvailabilityStatus = "Available");

public record ApprovedBudgetPlanPoStatusResponse(
    long BudgetPlanId,
    string BudgetPlanCode,
    string? Remark,
    DateTime DocDate,
    string BudgetPlanStatus,
    string BudgetPlanStatusDisplay,
    bool HasRfbaItems,
    long? VendorShadowId,
    string? VendorCode,
    string? VendorName,
    string? MakerName,
    string? ApprovalName,
    List<PoLinkInfo> PurchaseOrders,
    string? Location,
    decimal TotalBudgetPlan,
    decimal BudgetApproved,
    decimal BudgetVariance,
    bool AllGenerated = false);

public record RecapPurchaseOrderDetailResponse(
    long Id,
    string Code,
    string VendorName,
    string Status,
    string? Remark,
    DateTime DocDate,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? GeneratedAt,
    string? GeneratedByName,
    List<BpLinkInfo> LinkedBudgetPlans,
    List<PurchaseOrderItemResponse> Items,
    decimal GrandTotal,
    int TotalItems);
