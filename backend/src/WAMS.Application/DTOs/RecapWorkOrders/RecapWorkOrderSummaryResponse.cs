namespace WAMS.Application.DTOs.RecapWorkOrders;

public record RecapWorkOrderSummaryResponse(
    long Id,
    long BudgetPlanId,
    string BudgetPlanCode,
    string TemplateCode,
    string? Remark,
    string WarehouseCode,
    string WarehouseName,
    string? BlNumbers,
    string? ActivityTypes,
    string? PicNames,
    bool IsRfba,
    DateTime DocDate,
    string RecapStatus,
    DateTime CreatedAt);
