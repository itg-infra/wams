namespace WAMS.Application.DTOs.FinanceReports;

public record FinanceReportHeaderResponse(
    long BudgetPlanId,
    string BudgetNo,
    string TemplateId,
    string Status,
    string? Remark,
    DateTime DocDate,
    string WarehouseCode,
    string WarehouseName,
    string? Location);

public record FinanceReportCostDetailResponse(
    long PurchaseOrderItemId,
    string? WorkOrderId,
    string? BlNumber,
    string? Vessel,
    string Product,
    string? Pic,
    bool IsRfba,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal TotalPrice,
    bool IsPpnApplied,
    decimal PpnRatePercent,
    decimal TotalPricePpn,
    bool IsPphApplied,
    string? PphType,
    decimal TotalPricePph,
    decimal GrandTotal,
    string PaymentStatus);

public record FinanceReportBudgetRecapResponse(
    decimal BudgetPlan,
    decimal BudgetRealization,
    decimal BudgetVariance);

public record FinanceReportDetailResponse(
    FinanceReportHeaderResponse Header,
    List<FinanceReportCostDetailResponse> CostDetails,
    decimal Dpp,
    decimal TotalPpn,
    decimal TotalPph,
    decimal GrandTotal,
    FinanceReportBudgetRecapResponse BudgetRecap);
