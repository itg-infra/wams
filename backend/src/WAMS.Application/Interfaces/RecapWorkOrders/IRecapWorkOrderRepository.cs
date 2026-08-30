namespace WAMS.Application.Interfaces.RecapWorkOrders;

using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Domain.Entities.RecapWorkOrders;

public interface IRecapWorkOrderRepository
{
    Task UpsertForBudgetPlanAsync(long budgetPlanId, long companyId, CancellationToken ct = default);
    Task<(List<RecapWorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        RecapWorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default
    );

    Task<RecapWorkOrder?> GetByIdWithDetailsAsync(long id, CancellationToken ct = default);

    Task<RecapDetailProjection?> GetDetailProjectionAsync(long id, string? reviewerNameOverride, CancellationToken ct = default);

    Task ReviewAsync(long id, string status, long reviewedByUserId, DateTime reviewedAt, string? rejectionReason, CancellationToken ct = default);
    Task<bool> IsApprovedByBudgetPlanIdAsync(long budgetPlanId, CancellationToken ct = default);

    Task ResetToPendingByBudgetPlanIdAsync(long budgetPlanId, CancellationToken ct = default);

    IAsyncEnumerable<RecapWorkOrderSummaryResponse> StreamAllAsync(
        RecapWorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        CancellationToken ct = default);
}

public sealed record RecapDetailProjection(
    long Id,
    long BudgetPlanId,
    long CompanyId,
    long WarehouseShadowId,
    string RecapStatus,
    string? ReviewerName,
    DateTime? ReviewedAt,
    string? RejectionReason,
    RecapDetailHeader Header,
    IReadOnlyList<RecapDetailSpkRow> SpkRows,
    IReadOnlyList<RecapDetailCostRow> CostRows,
    IReadOnlyList<RecapDetailWoRow> WoRows);

public sealed record RecapDetailHeader(
    string BpCode,
    string TemplateCode,
    string BpStatus,
    string? Remark,
    DateTime DocDate,
    string WarehouseCode,
    string WarehouseName,
    string? WarehouseLocation);

public sealed record RecapDetailSpkRow(
    string Type,
    string DocNo,
    string BaseDocNo,
    string? BlNo,
    string ItemCode,
    string ItemName,
    decimal? Quantity,
    decimal? DeliveryQty,
    string UoM,
    int SortOrder);

public sealed record RecapDetailCostRow(
    long Id,
    string Type,
    string VendorCode,
    string VendorName,
    bool IsRfba,
    string? DocExternal,
    string ItemName,
    string AcctCode,
    string AcctName,
    string? BillOfLading,
    decimal CostValue,
    decimal Quantity,
    string UomCode,
    string? Description,
    decimal TotalValue,
    long ItemShadowId,
    int SortOrder);

public sealed record RecapDetailWoRow(
    long Id,
    string Code,
    string? BlNumber,
    string? PicName,
    bool IsRfba,
    DateTime? StartDate,
    DateTime? EndDate,
    string Status,
    string? ActivityName,
    string? VehicleNo,
    string ActivityTypeCode,
    long ItemShadowId,
    decimal UnloadingNettSum,
    decimal LoadingNettSum,
    decimal? StorageVolumeWeight,
    decimal? HeavyEquipTotalCost,
    decimal? UnbaggingTotalWeight,
    decimal? RebaggingTotalWeight,
    DateTime CreatedAt);
