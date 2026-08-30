namespace WAMS.Application.DTOs.RecapWorkOrders;

public record RecapBpHeaderResponse(
    string BudgetNo,
    string TemplateCode,
    string BudgetPlanStatus,
    string? Remark,
    DateTime DocDate,
    string WarehouseCode,
    string WarehouseName,
    string? Location);

public record RecapSpkDocumentResponse(
    string SpkType,
    string SpkNo,
    string DocumentNo,
    string? BlNo,
    string ItemCode,
    string ItemName,
    decimal? Quantity,
    decimal? DeliveryQty,
    string UoM);

public record RecapCostDetailResponse(
    string Type,
    string VendorCode,
    string VendorName,
    bool IsRfba,
    string? DocExternal,
    string CostName,
    string CoaCode,
    string CoaName,
    string? BillOfLading,
    decimal UnitCost,
    decimal UnitCount,
    string UomCode,
    string? Description,
    decimal TotalValue);

public record RecapPlanResponse(
    RecapBpHeaderResponse Header,
    List<RecapSpkDocumentResponse> SpkDocuments,
    List<RecapCostDetailResponse> CostDetails,
    decimal BudgetPlanTotal,
    decimal BudgetRealization,
    decimal BudgetVariance);

public record RecapWoItemResponse(
    long WorkOrderId,
    string WorkOrderCode,
    string? BlNumber,
    string? PicName,
    bool IsRfba,
    DateTime? StartDate,
    DateTime? EndDate,
    decimal ActualCost,
    string WorkOrderStatus,
    string? Product,
    string? VehicleNo);

public record RecapRealizationResponse(
    RecapBpHeaderResponse Header,
    List<RecapWoItemResponse> WorkOrders,
    decimal BudgetPlanTotal,
    decimal BudgetRealization,
    decimal BudgetVariance,
    decimal RealizationPercent);

public record RecapWorkOrderDetailResponse(
    long Id,
    long BudgetPlanId,
    string RecapStatus,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    string? RejectionReason,
    RecapPlanResponse Plan,
    RecapRealizationResponse Realization);

public record RejectRecapRequest(string? Reason);
