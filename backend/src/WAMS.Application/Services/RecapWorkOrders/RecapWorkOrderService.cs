namespace WAMS.Application.Services.RecapWorkOrders;

using System.Text.Json;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class RecapWorkOrderService(
    IRecapWorkOrderRepository recapRepo,
    IBudgetPlanRepository budgetPlanRepo,
    IUserRepository userRepo,
    IRbacService rbacService,
    IWarehouseContext warehouseContext,
    IWamsMetrics metrics,
    IAuditLogWriter auditLogWriter
) : IRecapWorkOrderService
{
    public async Task<(List<RecapWorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        RecapWorkOrderQuery q,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await recapRepo.GetAllAsync(q, warehouseIds, ct);
    }

    public async IAsyncEnumerable<RecapWorkOrderSummaryResponse> StreamAllAsync(
        RecapWorkOrderQuery q,
        long userId,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        await foreach (var item in recapRepo.StreamAllAsync(q, warehouseIds, limit, ct))
        {
            yield return item;
        }
    }

    public async Task<RecapWorkOrderDetailResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default)
    {
        var projection = await recapRepo.GetDetailProjectionAsync(id, reviewerNameOverride: null, ct)
            ?? throw new NotFoundException(ErrorMessages.RecapWorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(projection.WarehouseShadowId, userId, ct);

        return MapProjection(projection);
    }

    public async Task<RecapWorkOrderDetailResponse> ApproveAsync(
        long id,
        long userId,
        string? reviewerName,
        CancellationToken ct = default
    )
    {
        // Write paths still need the tracked entity graph so the threshold check can reuse the
        // existing in-memory math against bp.Items + bp.WorkOrders. The response is then served
        // by the same projection used by GET - avoids a 2nd mapping path drifting from MapProjection.
        var recap = await recapRepo.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.RecapWorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(recap.BudgetPlan.WarehouseShadowId, userId, ct);

        if (!recap.Status.CanBeReviewed)
            throw new ValidationException(ErrorMessages.RecapWorkOrder.CannotApproveOnlyPending);

        var draftCount = recap.BudgetPlan.WorkOrders.Count(w => w.DeletedAt == null && w.Status == WorkOrderStatus.Draft);

        if (draftCount > 0)
            throw new ValidationException(ErrorMessages.RecapWorkOrder.CannotApproveHasDraftWorkOrders(draftCount));

        var reviewedAt = DateTime.UtcNow;
        await recapRepo.ReviewAsync(recap.Id, RecapWorkOrderStatus.Approved.Value, userId, reviewedAt, null, ct);

        metrics.RecordRecapWorkOrderApproved(recap.CompanyId);

        var projection = await recapRepo.GetDetailProjectionAsync(id, reviewerName, ct)
            ?? throw new NotFoundException(ErrorMessages.RecapWorkOrder.NotFound(id));

        return MapProjection(projection);
    }

    public async Task<RecapWorkOrderDetailResponse> RejectAsync(
        long id,
        long userId,
        string? reviewerName,
        string? reason,
        CancellationToken ct = default
    )
    {
        var recap = await recapRepo.GetByIdWithDetailsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.RecapWorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(recap.BudgetPlan.WarehouseShadowId, userId, ct);

        if (!recap.Status.CanBeReviewed)
            throw new ValidationException(ErrorMessages.RecapWorkOrder.CannotRejectOnlyPending);

        var reviewedAt = DateTime.UtcNow;
        await recapRepo.ReviewAsync(recap.Id, RecapWorkOrderStatus.Rejected.Value, userId, reviewedAt, reason, ct);
        await budgetPlanRepo.RejectViaRecapAsync(recap.BudgetPlanId, userId, reviewedAt, reason, ct);

        await auditLogWriter.LogAsync(
            action: "UPDATE",
            tableName: "recap_work_orders",
            recordId: recap.Id,
            userId: userId,
            companyId: recap.CompanyId,
            newValues: JsonSerializer.Serialize(new { Status = "Rejected", Reason = reason }),
            ct: ct
        );

        await auditLogWriter.LogAsync(
            action: "UPDATE",
            tableName: "budget_plans",
            recordId: recap.BudgetPlanId,
            userId: userId,
            companyId: recap.CompanyId,
            newValues: JsonSerializer.Serialize(new { Status = "Rejected", RejectionReason = reason }),
            ct: ct
        );

        metrics.RecordRecapWorkOrderRejected(recap.CompanyId);

        var projection = await recapRepo.GetDetailProjectionAsync(id, reviewerName, ct)
            ?? throw new NotFoundException(ErrorMessages.RecapWorkOrder.NotFound(id));

        return MapProjection(projection);
    }

    private async Task EnsureWarehouseAccessAsync(long warehouseShadowId, long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            if (warehouseShadowId != warehouseContext.WarehouseId.Value)
                throw new ForbiddenException(ErrorMessages.RecapWorkOrder.AccessDeniedDifferentWarehouse);
            return;
        }

        if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
            {
                var ids = await userRepo.GetUserWarehouseIdsAsync(userId, ct);
                if (!ids.Contains(warehouseShadowId))
                    throw new ForbiddenException(ErrorMessages.RecapWorkOrder.AccessDeniedDifferentWarehouse);
            }
        }
    }

    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
            return [warehouseContext.WarehouseId.Value];

        if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
                return (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();
        }

        return null;
    }

    private static RecapWorkOrderDetailResponse MapProjection(RecapDetailProjection p)
    {
        var headerDto = new RecapBpHeaderResponse(
            p.Header.BpCode,
            p.Header.TemplateCode,
            p.Header.BpStatus,
            p.Header.Remark,
            p.Header.DocDate,
            p.Header.WarehouseCode,
            p.Header.WarehouseName,
            p.Header.WarehouseLocation);

        var spkDocs = p.SpkRows
            .Select(s => new RecapSpkDocumentResponse(
                s.Type,
                s.DocNo,
                s.BaseDocNo,
                s.BlNo,
                s.ItemCode,
                s.ItemName,
                s.Quantity,
                s.DeliveryQty,
                s.UoM))
            .ToList();

        var costDetails = p.CostRows
            .Select(c => new RecapCostDetailResponse(
                c.Type,
                c.VendorCode,
                c.VendorName,
                c.IsRfba,
                c.DocExternal,
                c.ItemName,
                c.AcctCode,
                c.AcctName,
                c.BillOfLading,
                c.CostValue,
                c.Quantity,
                c.UomCode,
                c.Description,
                c.TotalValue))
            .ToList();

        var budgetPlanTotal = p.CostRows.Sum(c => c.TotalValue);
        var bpBlNo = p.SpkRows.FirstOrDefault()?.BlNo;

        // Group cost rows by item shadow once - reused for every WO actual-cost computation.
        // Replaces the O(N×M) lookup in the old in-memory mapper.
        var costByItem = p.CostRows
            .GroupBy(c => c.ItemShadowId)
            .ToDictionary(
                g => g.Key,
                g => (
                    Total: g.Sum(c => c.TotalValue),
                    Qty: g.Sum(c => c.Quantity)
                )
            );

        var woItems = p.WoRows
            .Select(w => new RecapWoItemResponse(
                w.Id,
                w.Code,
                bpBlNo,
                w.PicName,
                w.IsRfba,
                w.StartDate,
                w.EndDate,
                ComputeActualCostFromProjection(w, costByItem),
                w.Status,
                w.ActivityName,
                w.VehicleNo))
            .ToList();

        var budgetRealization = woItems.Sum(w => w.ActualCost);
        var budgetVariance = budgetPlanTotal - budgetRealization;
        var realizationPercent = budgetPlanTotal > 0
            ? Math.Round(budgetRealization / budgetPlanTotal * 100m, 2)
            : 0m;

        var plan = new RecapPlanResponse(
            headerDto, 
            spkDocs, 
            costDetails, 
            budgetPlanTotal, 
            budgetRealization, 
            budgetVariance
        );
        var realization = new RecapRealizationResponse(
            headerDto, 
            woItems, 
            budgetPlanTotal, 
            budgetRealization, 
            budgetVariance, 
            realizationPercent
        );

        return new RecapWorkOrderDetailResponse(
            p.Id,
            p.BudgetPlanId,
            p.RecapStatus,
            p.ReviewerName,
            p.ReviewedAt,
            p.RejectionReason,
            plan,
            realization
        );
    }

    private static decimal ComputeActualCostFromProjection(
        RecapDetailWoRow wo,
        IReadOnlyDictionary<long, (decimal Total, decimal Qty)> costByItem)
    {
        var planned = costByItem.GetValueOrDefault(wo.ItemShadowId);
        var rate = planned.Qty > 0 ? planned.Total / planned.Qty : 0m;

        return wo.ActivityTypeCode switch
        {
            ActivityTypeCodes.Bongkar => rate * wo.UnloadingNettSum,
            ActivityTypeCodes.Muat => rate * wo.LoadingNettSum,
            ActivityTypeCodes.Gudang => rate * (wo.StorageVolumeWeight ?? 0m),
            ActivityTypeCodes.AlatBerat => wo.HeavyEquipTotalCost ?? 0m,
            ActivityTypeCodes.Unbagging => rate * (wo.UnbaggingTotalWeight ?? 0m),
            ActivityTypeCodes.Rebagging => rate * (wo.RebaggingTotalWeight ?? 0m),
            _ => planned.Total, // fixed-fee activities
        };
    }
}