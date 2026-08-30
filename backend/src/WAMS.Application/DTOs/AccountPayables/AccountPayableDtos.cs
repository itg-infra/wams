namespace WAMS.Application.DTOs.AccountPayables;

using WAMS.Application.Common;

public record AccountPayableQuery : DataTableQuery
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public long? VendorShadowId { get; init; }
    public string? Status { get; init; }
}

public record CreateAccountPayableRequest(
    long VendorShadowId,
    string? Remark,
    DateTime DocDate,
    List<long> Items,
    decimal DiscountAmount = 0m);

public record UpdateAccountPayableRequest(
    string? Remark,
    DateTime? DocDate,
    List<long>? Items,
    decimal? DiscountAmount = null);

public record AccountPayableSummaryResponse(
    long Id,
    string Code,
    string VendorCode,
    string VendorName,
    string Status,
    DateTime DocDate,
    string? Remark,
    string? SapApNumber,
    decimal GrandTotal,
    int ItemCount,
    DateTime CreatedAt,
    string CreatedByName);

public record ApBudgetPlanLinkInfo(long Id, string Code);

public record AccountPayableResponse(
    long Id,
    string Code,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    string Status,
    DateTime DocDate,
    string? Remark,
    string? SapApNumber,
    List<string> LinkedBudgetPlanCodes,
    List<ApBudgetPlanLinkInfo> LinkedBudgetPlans,
    List<AccountPayableItemResponse> Items,
    decimal GrandTotal,
    decimal TotalPpnAmount,
    decimal TotalPphAmount,
    decimal TaxInclusiveGrandTotal,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? GeneratedAt,
    string? GeneratedByName,
    decimal DiscountAmount,
    decimal DiscountPercent,
    decimal TotalRealization,
    decimal TotalVariance,
    List<string>? Warnings = null);

public record AccountPayableItemResponse(
    long Id,
    long BudgetPlanItemId,
    long BudgetPlanId,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    string ItemCode,
    string ItemName,
    string CoaCode,
    string CoaName,
    string UomCode,
    string UomName,
    bool IsRfba,
    string? BillOfLading,
    decimal UnitCost,
    decimal UnitCount,
    decimal BudgetPlanTotal,
    decimal BudgetRealization,
    decimal BudgetVariance,
    int SortOrder,
    string? PpnTaxTypeCode,
    decimal PpnRate,
    string? PphTaxTypeCode,
    decimal PphRate,
    decimal PpnAmount,
    decimal PphAmount,
    decimal GrandTotal,
    string? CostTreatment);

public record PreviewAccountPayableRequest(
    long VendorShadowId,
    List<long> Items,
    decimal DiscountAmount);

public record AccountPayableTotalsResponse(
    List<AccountPayableItemResponse> Items,
    decimal DppTotal,
    decimal TotalPpnAmount,
    decimal TotalPphAmount,
    decimal TaxInclusiveGrandTotal,
    decimal DiscountAmount,
    decimal DiscountPercent,
    decimal TotalRealization,
    decimal TotalVariance);

public record AvailableApItemResponse(
    long BudgetPlanItemId,
    long BudgetPlanId,
    string BudgetPlanCode,
    string? BudgetPlanRemark,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    string ItemCode,
    string ItemName,
    string CoaCode,
    string CoaName,
    string UomCode,
    string UomName,
    bool IsRfba,
    string? BillOfLading,
    decimal UnitCost,
    decimal UnitCount,
    decimal BudgetPlanTotal,
    bool IsGenerated,
    string? TakenByCode,
    string AvailabilityStatus = "Available");

public record BudgetPlanItemAvailability(
    long Id,
    bool Found,
    bool VendorMatches,
    bool WarehouseInScope,
    bool RecapApproved,
    bool AlreadyGenerated,
    string? TakenByCode,
    long? ActualVendorShadowId = null);

public record ApLinkInfo(
    long Id,
    string Code,
    string Status,
    string? SapApNumber,
    string VendorCode);

public record ApprovedRecapApStatusResponse(
    long RecapWorkOrderId,
    long BudgetPlanId,
    string BudgetPlanCode,
    string? Remark,
    DateTime DocDate,
    bool HasRfbaItems,
    long? VendorShadowId,
    string? VendorCode,
    string? VendorName,
    decimal BudgetPlanTotal,
    List<ApLinkInfo> AccountPayables,
    bool IsAllGenerated,
    string? Location,
    decimal BudgetApproved,
    decimal BudgetVariance);
