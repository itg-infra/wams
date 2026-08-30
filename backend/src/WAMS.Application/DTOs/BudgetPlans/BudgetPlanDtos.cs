namespace WAMS.Application.DTOs.BudgetPlans;

using WAMS.Application.Common;
using WAMS.Domain.Enums;

public record AddSpkItemRequest(long SpkShadowId);

public record BudgetPlanSpkItemResponse(
    long Id,
    long SpkShadowId,
    string Type,
    string DocNo,
    string BaseDoc,
    string BaseDocNo,
    string CardCode,
    string CardName,
    string ItemCode,
    string ItemName,
    decimal? Quantity,
    decimal? DeliveryQty,
    string UoM,
    string PackType,
    string WhsCode,
    string WhsName,
    string DocStatus,
    string? BlNo,
    int SortOrder,
    long? ItemShadowId);

public record BudgetPlanQuery : DataTableQuery
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}

public record CreateBudgetPlanItemRequest(
    long ItemShadowId,
    long ActivityTypeId,
    long VendorShadowId,
    decimal Quantity,
    decimal? CostValue,
    BudgetPlanType Type,
    bool IsRfba,
    string? BillOfLading,
    string? Description,
    long? SpkShadowId,
    long? UomMasterId = null);

public record CreateBudgetPlanRequest(
    long BudgetTemplateId,
    long WarehouseShadowId,
    string? Remark,
    DateTime DocDate,
    List<CreateBudgetPlanItemRequest> Items,
    List<long>? SpkShadowIds);

public record UpdateBudgetPlanRequest(
    long? WarehouseShadowId,
    string? Remark,
    DateTime? DocDate,
    List<CreateBudgetPlanItemRequest>? Items,
    List<long>? SpkShadowIds);

public record RejectBudgetPlanRequest(string Reason);

public record BudgetPlanSummaryResponse(
    long Id,
    string BudgetNo,
    string TemplateCode,
    string? Remark,
    string? Location,
    string? VendorName,
    string? MakerName,
    DateTime DocDate,
    string Status,
    string StatusDisplay,
    BudgetPlanApprovalInfo Approval);

public record WorkflowStageInfo(
    int StageOrder,
    string StageName,
    string[] ApproverRoles,
    string Status,
    DateTime? ApprovedAt,
    string? ApprovedByName,
    DateTime? RejectedAt,
    string? RejectedByName,
    string? RejectionReason);

public record BudgetPlanApprovalInfo(
    int TotalStages,
    int CurrentStageOrder,
    List<WorkflowStageInfo> Stages);

public record BudgetPlanResponse(
    long Id,
    string BudgetNo,
    BudgetTemplateSummaryInfo Template,
    string WarehouseCode,
    string WarehouseName,
    string? Remark,
    DateTime DocDate,
    string Status,
    string StatusDisplay,
    List<BudgetPlanSpkItemResponse> SpkItems,
    List<BudgetPlanItemResponse> Items,
    decimal GrandTotal,
    decimal TotalPpnAmount,
    decimal TotalPphAmount,
    decimal TaxInclusiveGrandTotal,
    DateTime CreatedAt,
    string CreatedByName,
    DateTime? SubmittedAt,
    string? SubmittedByName,
    BudgetPlanApprovalInfo Approval,
    DateTime? RejectedAt,
    string? RejectedByName,
    string? RejectionReason);

public record BudgetPlanItemResponse(
    long Id,
    long ItemShadowId,
    string CostDetail,
    string CostName,
    string Coa,
    string CoaName,
    long VendorShadowId,
    string VendorCode,
    string VendorName,
    long UomMasterId,
    string UomCode,
    string UomName,
    decimal CostValue,
    decimal Quantity,
    decimal TotalValue,
    int SortOrder,
    string Type,
    bool IsRfba,
    string? DocExternal,
    string? BillOfLading,
    string? Description,
    long ActivityTypeId,
    string? ActivityTypeCode,
    string? ActivityTypeName,
    long? SpkShadowId,
    string? PpnTaxTypeCode,
    decimal PpnRate,
    string? PphTaxTypeCode,
    decimal PphRate,
    decimal PpnAmount,
    decimal PphAmount,
    decimal GrandTotal,
    string? CostTreatment);

public record BudgetTemplateSummaryInfo(
    long Id,
    string TemplateCode,
    long? ProvinceId,
    string? ProvinceName,
    string? ProvinceDisplay);

/// <summary>
/// Minimal projection of a BudgetPlan used by the WorkOrder create path. Returned by
/// <c>IBudgetPlanRepository.GetForWoCreateAsync</c>. Carries only the fields needed for
/// validation + WO construction; avoids the 10-Include AsSplitQuery load.
/// </summary>
public record BpItemForWo(long Id, long ItemShadowId, string ActivityTypeCode);

public record BpForWoCreateProjection(
    long Id,
    string Status,
    long CompanyId,
    long WarehouseShadowId,
    string TemplateCode,
    List<BpItemForWo> Items,
    bool AnyRfba);
