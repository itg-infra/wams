namespace WAMS.Application.Services.PurchaseOrders;

using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class PurchaseOrderService(
    IPurchaseOrderRepository poRepo,
    IBudgetPlanRepository bpRepo,
    IVendorShadowRepository vendorRepo,
    ISapApiClient sapClient,
    IUnitOfWork uow,
    IWarehouseContext warehouseContext,
    IWarehouseShadowRepository warehouseRepo,
    IUserRepository userRepo,
    IRbacService rbacService,
    ICodeCounterRepository codeCounterRepo
) : IPurchaseOrderService
{
    public Task<(List<PurchaseOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        PurchaseOrderQuery q,
        CancellationToken ct = default
    )
        => poRepo.GetAllAsync(q, ct);

    public IAsyncEnumerable<PurchaseOrderSummaryResponse> StreamAllAsync(
        PurchaseOrderQuery q,
        int limit,
        CancellationToken ct = default
    )
        => poRepo.StreamAllAsync(q, limit, ct);

    public async Task<PurchaseOrderResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var po = await poRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

        var bpIds = po.Items
            .Select(i => i.BudgetPlanItem.BudgetPlanId)
            .Distinct()
            .ToList();

        var siblings = await poRepo.GetPoSummariesByBudgetPlanIdsAsync(bpIds, excludePoId: id, ct);

        return MapDetail(po, siblings);
    }

    public async Task<RecapPurchaseOrderDetailResponse> GetRecapDetailAsync(
        bool isRfba,
        long id,
        CancellationToken ct = default
    )
    {
        var po = await poRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

        var matchingItems = po.Items.Where(i => i.IsRfba == isRfba).OrderBy(i => i.SortOrder).ToList();

        var bpIds = matchingItems
            .Select(i => i.BudgetPlanItem.BudgetPlanId)
            .Distinct()
            .ToList();

        var siblings = bpIds.Count == 0
            ? []
            : await poRepo.GetPoSummariesByBudgetPlanIdsAsync(bpIds, excludePoId: id, ct);

        return MapRecapDetail(po, matchingItems, siblings);
    }

    public async Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsAsync(
        long userId,
        AvailablePoItemQuery query,
        CancellationToken ct = default
    )
    {
        if (!query.VendorShadowId.HasValue)
            throw new ValidationException(ErrorMessages.Validation.Common.VendorRequired);

        var activeWarehouseId = warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue
            ? warehouseContext.WarehouseId.Value
            : (long?)null;
        var warehouseIds = await ResolvePoAccessibleWarehouseIdsAsync(userId, ct);
        var seed = await bpRepo.GetByIdWithItemsAsync(query.BudgetPlanId, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(query.BudgetPlanId));

        if (activeWarehouseId.HasValue && seed.WarehouseShadowId != activeWarehouseId.Value)
            throw new ValidationException(
                ErrorMessages.PurchaseOrder.SeedBudgetPlanWarehouseMismatch(seed.Id, activeWarehouseId.Value));

        if (seed.Status != BudgetPlanStatus.Approved || seed.DeletedAt is not null)
            throw new ValidationException(ErrorMessages.PurchaseOrder.ItemPlanNotApproved(seed.Id));

        var vendorShadowId = query.VendorShadowId.Value;
        var seedVendorIds = seed.Items
            .Select(item => item.VendorShadowId)
            .Distinct()
            .ToList();

        if (!seedVendorIds.Contains(vendorShadowId))
            throw new ValidationException(
                ErrorMessages.PurchaseOrder.SeedVendorMismatch(vendorShadowId, seed.Id));

        return await poRepo.GetAvailableItemsForPickerAsync(
            [vendorShadowId],
            seed.Id,
            ToDataTableQuery(query),
            query.IncludeGenerated,
            warehouseIds: warehouseIds?.ToList(),
            ct: ct);
    }

    public async Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsForEditAsync(
        long userId,
        long purchaseOrderId,
        EditAvailablePoItemQuery query,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolvePoAccessibleWarehouseIdsAsync(userId, ct);
        var po = await poRepo.GetByIdWithItemsAsync(purchaseOrderId, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(purchaseOrderId));

        if (!po.Status.CanBeEdited)
            throw new ValidationException(ErrorMessages.PurchaseOrder.CannotUpdateOnlyDraft);

        var sourceWarehouseIds = po.Items
            .Select(item => item.BudgetPlanItem.BudgetPlan.WarehouseShadowId)
            .Distinct()
            .ToList();
        await EnsurePoWarehouseAccessAsync(userId, sourceWarehouseIds, ct);

        var linkedBudgetPlanIds = po.Items
            .Select(item => item.BudgetPlanItem.BudgetPlanId)
            .ToHashSet();

        var (items, total) = await poRepo.GetAvailableItemsForPickerAsync(
            [po.VendorShadowId],
            seedBudgetPlanId: null,
            ToDataTableQuery(query),
            query.IncludeGenerated,
            excludeDocumentId: po.Id,
            warehouseIds: warehouseIds?.ToList(),
            ct: ct);

        return (
            [.. items.Select(item => item with
            {
                IsSeedBudgetPlan = linkedBudgetPlanIds.Contains(item.BudgetPlanId),
            })],
            total);
    }

    public async Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetApprovedBudgetPlansAsync(
        long userId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await poRepo.GetApprovedBudgetPlansWithPoStatusAsync(warehouseIds?.ToArray(), query, ct);
    }

    public async Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetRecapAsync(
        bool isRfba,
        long userId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await poRepo.GetRecapPurchaseOrdersAsync(isRfba, warehouseIds?.ToArray(), query, ct);
    }

    public async IAsyncEnumerable<ApprovedBudgetPlanPoStatusResponse> StreamRecapAsync(
        bool isRfba,
        long userId,
        DataTableQuery query,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        await foreach (
            var item in poRepo.StreamRecapPurchaseOrdersAsync(
                isRfba,
                warehouseIds?.ToArray(),
                query,
                limit,
                ct
            )
        )

            yield return item;
    }

    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);
            return [warehouseContext.WarehouseId.Value];
        }

        if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
                return (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();
        }

        return null;
    }

    private async Task<IReadOnlyList<long>?> ResolvePoAccessibleWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);

        if (await rbacService.HasGlobalAccessAsync(userId, ct))
            return null;

        return (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();
    }

    private static DataTableQuery ToDataTableQuery(DataTableQuery query) => new()
    {
        Search = query.Search,
        SortBy = query.SortBy,
        SortOrder = query.SortOrder,
        Page = query.Page,
        Limit = query.Limit,
    };

    private async Task EnsurePoWarehouseAccessAsync(long userId, IEnumerable<long> requiredWarehouseIds, CancellationToken ct)
    {
        var scope = await ResolvePoAccessibleWarehouseIdsAsync(userId, ct);
        if (scope is null) return;

        var allowed = scope.ToHashSet();
        if (requiredWarehouseIds.Any(id => !allowed.Contains(id)))
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }

    private async Task EnsureWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        _ = await warehouseRepo.GetByIdAsync(warehouseId, ct)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));

        if (await rbacService.HasGlobalAccessAsync(userId, ct)) return;

        var ids = await userRepo.GetUserWarehouseIdsAsync(userId, ct);

        if (!ids.Contains(warehouseId))
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }

    public async Task<PurchaseOrderResponse> CreateAsync(
        long userId,
        CreatePurchaseOrderRequest request,
        CancellationToken ct = default
    )
    {
        _ = await vendorRepo.GetByIdAsync(request.VendorShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(request.VendorShadowId));

        var warehouseIds = await ResolvePoAccessibleWarehouseIdsAsync(userId, ct);

        PurchaseOrder po = null!;

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            await poRepo.LockBudgetPlanItemsAsync(request.Items, innerCt);

            var availableItems = await poRepo.GetAvailableItemsAsync(
                request.VendorShadowId,
                request.Items,
                warehouseIds: warehouseIds?.ToList(),
                ct: innerCt
            );

            await ValidateAllItemsAvailableAsync(
                request.VendorShadowId,
                request.Items,
                availableItems,
                warehouseIds,
                innerCt
            );

            var prefix = $"PO-{DateTime.UtcNow:yyMM}";
            var code = await DocumentCodeGenerator.NextCodeAsync(codeCounterRepo, prefix, innerCt);

            po = new PurchaseOrder
            {
                Code = code,
                VendorShadowId = request.VendorShadowId,
                Remark = request.Remark,
                DocDate = request.DocDate,
                Status = PurchaseOrderStatus.Draft,
                CreatedByUserId = userId,
            };

            BuildItems(po, request.Items, availableItems);

            await poRepo.CreateAsync(po, innerCt);
            await uow.CommitAsync(innerCt);
        }, ct);

        var created = await poRepo.GetByIdWithItemsAsync(po.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFoundAfterCreation);

        return MapDetail(created, []);
    }

    public async Task<PurchaseOrderResponse> CreateAndGenerateAsync(
        long userId,
        CreatePurchaseOrderRequest request,
        CancellationToken ct = default
    )
    {
        if (request.Items.Count == 0)
            throw new ValidationException(ErrorMessages.PurchaseOrder.NoItemsCannotGenerate);

        var created = await CreateAsync(userId, request, ct);
        try
        {
            return await GenerateAsync(created.Id, userId, ct);
        }
        catch
        {
            // CreateAsync already committed this Draft it only exists to carry the SAP call, so
            // drop it on failure instead of orphaning it.
            await poRepo.SoftDeleteAsync(created.Id, ct);
            throw;
        }
    }

    public async Task<PurchaseOrderResponse> UpdateAsync(
        long id,
        long userId,
        UpdatePurchaseOrderRequest request,
        CancellationToken ct = default
    )
    {
        var po = await poRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

        if (!po.Status.CanBeEdited)
            throw new ValidationException(ErrorMessages.PurchaseOrder.CannotUpdateOnlyDraft);

        if (request.Items is not null && request.Items.Count == 0)
            throw new ValidationException(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        if (request.Remark is not null)
            po.Remark = request.Remark;

        if (request.DocDate.HasValue)
            po.DocDate = request.DocDate.Value;

        var warehouseIds = await ResolvePoAccessibleWarehouseIdsAsync(userId, ct);

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            if (!await poRepo.LockForEditAsync(id, innerCt))
                throw new ConflictException(ErrorMessages.PurchaseOrder.GenerationInProgress(id));

            if (request.Items is not null)
            {
                await poRepo.LockBudgetPlanItemsAsync(request.Items, innerCt);

                var availableItems = await poRepo.GetAvailableItemsAsync(
                    po.VendorShadowId,
                    request.Items,
                    excludeDocumentId: po.Id,
                    warehouseIds: warehouseIds?.ToList(),
                    ct: innerCt
                );

                await ValidateAllItemsAvailableAsync(
                    po.VendorShadowId,
                    request.Items,
                    availableItems,
                    warehouseIds,
                    innerCt
                );

                po.Items.Clear();

                BuildItems(po, request.Items, availableItems);
            }

            po.UpdatedAt = DateTime.UtcNow;

            await poRepo.UpdateAsync(po, innerCt);
            await uow.CommitAsync(innerCt);
        }, ct);

        var updated = await poRepo.GetByIdWithItemsAsync(id, ct)!;

        return MapDetail(updated!, []);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var po = await poRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

        if (!po.Status.CanBeDeleted)
            throw new ValidationException(ErrorMessages.PurchaseOrder.CannotDeleteOnlyDraft);

        if (!await poRepo.SoftDeleteAsync(id, ct))
            throw new ConflictException(ErrorMessages.PurchaseOrder.GenerationInProgress(id));

        await uow.CommitAsync(ct);
    }

    public async Task<PurchaseOrderResponse> GenerateAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var claimToken = Guid.NewGuid().ToString("N");
        PurchaseOrder po = null!;
        if (!await poRepo.TryClaimForGenerationAsync(id, claimToken, ct))
        {
            var current = await poRepo.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

            if (!current.Status.CanBeGenerated)
                throw new ValidationException(ErrorMessages.PurchaseOrder.CannotGenerateOnlyDraft);

            throw new ConflictException(ErrorMessages.PurchaseOrder.GenerationInProgress(id));
        }

        try
        {
            po = await poRepo.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(ErrorMessages.PurchaseOrder.NotFound(id));

            if (po.Items.Count == 0)
                throw new ValidationException(ErrorMessages.PurchaseOrder.NoItemsCannotGenerate);

            await EnsurePoWarehouseAccessAsync(
                userId,
                po.Items.Select(i => i.BudgetPlanItem.BudgetPlan.WarehouseShadowId).Distinct(),
                ct);

            var sapRequest = new SapCreatePoRequest(
                po.Code,
                po.Items.First().VendorCode,
                po.DocDate,
                po.Remark,
                [.. po.Items.OrderBy(i => i.SortOrder).Select(i => new SapPoLineItem(
                    i.ItemCode,
                    i.ItemName,
                    i.Quantity,
                    i.CostValue,
                    i.BudgetPlanItem.BudgetPlan.Warehouse.Code,
                    i.PpnTaxTypeCode,
                    i.BillOfLading,
                    i.BudgetPlanItem.Spk?.ItemCode))]
            );

            var sapResult = await sapClient.CreatePurchaseOrderAsync(sapRequest, ct)
                ?? throw new ValidationException(ErrorMessages.PurchaseOrder.SapNoPoNumber);
            var sapPoNumber = sapResult.SapPoNumber;
            var budgetPlanItemIds = po.Items.Select(i => i.BudgetPlanItemId).ToList();

            // Set doc external number and generated status in transaction
            await uow.ExecuteInTransactionAsync(async innerCt =>
            {
                if (!await poRepo.MarkGeneratedAsync(po.Id, claimToken, sapPoNumber, sapResult.SapDocEntry, userId, innerCt))
                    throw new ConflictException(ErrorMessages.PurchaseOrder.GenerationInProgress(id));

                await bpRepo.SetItemsDocExternalAsync(budgetPlanItemIds, sapPoNumber, innerCt);
            }, ct);

            po.Status = PurchaseOrderStatus.Generated;
            po.SapPoNumber = sapPoNumber;
            po.SapDocEntry = sapResult.SapDocEntry;
            po.GeneratedByUserId = userId;
            po.GeneratedAt = DateTime.UtcNow;
            po.GeneratedBy = await userRepo.GetByIdAsync(userId, ct);

            return MapDetail(po, []);
        }
        catch
        {
            // Compensating cleanup must run to completion even if the request's own ct
            // is already cancelled (e.g. client disconnect) otherwise this throws
            // OperationCanceledException before reaching `throw;`, replacing the original
            // exception and leaking the claim (never released, self-expires after the 15-minute lease).
            await poRepo.ReleaseGenerationClaimAsync(id, claimToken, CancellationToken.None);
            throw;
        }
    }

    private async Task ValidateAllItemsAvailableAsync(
        long vendorShadowId,
        List<long> requestedIds,
        List<BudgetPlanItem> availableItems,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct
    )
    {
        var availableSet = availableItems.Select(i => i.Id).ToHashSet();
        var missingIds = requestedIds.Where(id => !availableSet.Contains(id)).ToList();

        if (missingIds.Count == 0) return;

        var diagnostics = await poRepo.GetAvailabilityDiagnosticsAsync(vendorShadowId, missingIds, warehouseIds?.ToList(), ct);

        var invalidVendorItems = diagnostics
            .Where(d => d.Found && !d.VendorMatches)
            .Select(d => new InvalidPurchaseOrderItem(
                d.Id,
                vendorShadowId,
                d.ActualVendorShadowId))
            .ToList();

        var reasons = diagnostics.Select(d => d switch
        {
            { Found: false } => ErrorMessages.PurchaseOrder.ItemNotFound(d.Id),
            { VendorMatches: false } => ErrorMessages.PurchaseOrder.ItemVendorMismatch(d.Id),
            { WarehouseInScope: false } => ErrorMessages.PurchaseOrder.ItemWarehouseNotAccessible(d.Id),
            { AlreadyGenerated: true } => ErrorMessages.PurchaseOrder.ItemAlreadyGenerated(d.Id),
            { PlanApproved: false } => ErrorMessages.PurchaseOrder.ItemPlanNotApproved(d.Id),
            { TakenByCode: not null } => ErrorMessages.PurchaseOrder.ItemAlreadyTaken(d.Id, d.TakenByCode),
            _ => ErrorMessages.PurchaseOrder.ItemUnavailable(d.Id),
        });

        throw invalidVendorItems.Count > 0
            ? new ValidationException(
                string.Join(" ", reasons),
                ErrorCodes.PurchaseOrderItemVendorMismatch,
                new PurchaseOrderItemValidationDetails(invalidVendorItems))
            : new ValidationException(string.Join(" ", reasons));
    }

    // Tax fields (PpnAmount/PphAmount/GrandTotal/rates) and Quantity are copied verbatim from the
    // BudgetPlanItem snapshot rather than recomputed via TaxCalculator. This is only correct because
    // CreatePurchaseOrderRequest.Items/UpdatePurchaseOrderRequest.Items is a plain List<long> of
    // BudgetPlanItem ids with no per-item quantity override - a PO item always takes the full BP
    // item quantity. If partial-quantity PO generation is ever added, these amounts must be
    // recomputed via TaxCalculator.Calculate instead of copied.
    private static void BuildItems(
        PurchaseOrder po,
        List<long> itemIds,
        List<BudgetPlanItem> availableItems
    )
    {
        var lookup = availableItems.ToDictionary(i => i.Id);

        for (var i = 0; i < itemIds.Count; i++)
        {
            var bpi = lookup[itemIds[i]];
            po.Items.Add(new PurchaseOrderItem
            {
                BudgetPlanItemId = bpi.Id,
                ItemShadowId = bpi.ItemShadowId,
                ItemCode = bpi.Item.ItemCode,
                ItemName = bpi.Item.ItemName,
                CoaCode = bpi.Item.AcctCode,
                CoaName = bpi.Item.AcctName,
                VendorShadowId = bpi.VendorShadowId,
                VendorCode = bpi.Vendor.CardCode,
                VendorName = bpi.Vendor.CardName,
                UomMasterId = bpi.UomMasterId,
                UomCode = bpi.Uom.Code,
                UomName = bpi.Uom.Name,
                IsRfba = bpi.IsRfba,
                BillOfLading = bpi.BillOfLading,
                CostValue = bpi.CostValue,
                Quantity = bpi.Quantity,
                TotalValue = bpi.CostValue * bpi.Quantity,
                SortOrder = i + 1,
                PpnTaxTypeCode = bpi.PpnTaxTypeCode,
                PpnRate = bpi.PpnRate,
                PphTaxTypeCode = bpi.PphTaxTypeCode,
                PphRate = bpi.PphRate,
                PpnAmount = bpi.PpnAmount,
                PphAmount = bpi.PphAmount,
                GrandTotal = bpi.GrandTotal,
                CostTreatment = bpi.CostTreatment,
            });
        }
    }

    private static PurchaseOrderResponse MapDetail(
        PurchaseOrder p,
        List<(long BudgetPlanId, long PoId, string PoCode)> siblings
    )
    {
        var siblingsByBp = siblings
            .GroupBy(s => s.BudgetPlanId)
            .ToDictionary(g => g.Key, g => g.Select(s => new PoLinkInfo(s.PoId, s.PoCode)).ToList());

        var sourceBudgetPlans = p.Items
            .Select(i => i.BudgetPlanItem.BudgetPlan)
            .DistinctBy(bp => bp.Id)
            .OrderBy(bp => bp.Code)
            .ToList();

        var linkedBudgetPlans = sourceBudgetPlans
            .Select(bp => new BpLinkInfo(
                bp.Id,
                bp.Code,
                siblingsByBp.GetValueOrDefault(bp.Id, [])))
            .ToList();

        // A PO has no approval of its own - the only approval upstream of it is the
        // source budget plan's. A PO can span more than one BP (multi-warehouse case)
        // but the form has one signature block, so take the first BP's workflow.
        IReadOnlyList<PoApprover> approvers = [.. (sourceBudgetPlans.FirstOrDefault()?.WorkflowInstance?.Stages ?? [])
            .OrderBy(s => s.StageOrder)
            .Select(s => new PoApprover(s.ApprovedBy?.Fullname, s.ApprovedAt))];

        return new(
            p.Id,
            p.Code,
            p.VendorShadowId,
            p.Vendor.CardCode,
            p.Vendor.CardName,
            p.Status.Value,
            p.DocDate,
            p.Remark,
            p.SapPoNumber,
            linkedBudgetPlans,
            [.. p.Items.OrderBy(i => i.SortOrder).Select(i => new PurchaseOrderItemResponse(
                i.Id,
                i.BudgetPlanItemId,
                i.ItemShadowId,
                i.ItemCode,
                i.ItemName,
                i.CoaCode,
                i.CoaName,
                i.VendorShadowId,
                i.VendorCode,
                i.VendorName,
                i.UomMasterId,
                i.UomCode,
                i.UomName,
                i.IsRfba,
                i.BillOfLading,
                i.CostValue,
                i.Quantity,
                i.TotalValue,
                i.SortOrder,
                i.PpnTaxTypeCode,
                i.PpnRate,
                i.PphTaxTypeCode,
                i.PphRate,
                i.PpnAmount,
                i.PphAmount,
                i.GrandTotal,
                i.CostTreatment))],
            p.Items.Sum(i => i.TotalValue),
            p.Items.Sum(i => i.PpnAmount),
            p.Items.Sum(i => i.PphAmount),
            p.Items.Sum(i => i.GrandTotal),
            p.CreatedAt,
            p.CreatedBy.Fullname,
            p.GeneratedAt,
            p.GeneratedBy?.Fullname,
            approvers
        );
    }

    private static RecapPurchaseOrderDetailResponse MapRecapDetail(
        PurchaseOrder p,
        List<PurchaseOrderItem> matchingItems,
        List<(long BudgetPlanId, long PoId, string PoCode)> siblings
    )
    {
        var siblingsByBp = siblings
            .GroupBy(s => s.BudgetPlanId)
            .ToDictionary(g => g.Key, g => g.Select(s => new PoLinkInfo(s.PoId, s.PoCode)).ToList());

        var linkedBudgetPlans = matchingItems
            .Select(i => i.BudgetPlanItem.BudgetPlan)
            .DistinctBy(bp => bp.Id)
            .OrderBy(bp => bp.Code)
            .Select(bp => new BpLinkInfo(
                bp.Id,
                bp.Code,
                siblingsByBp.GetValueOrDefault(bp.Id, [])))
            .ToList();

        return new(
            p.Id,
            p.Code,
            p.Vendor.CardName,
            p.Status.Value,
            p.Remark,
            p.DocDate,
            p.CreatedAt,
            p.CreatedBy.Fullname,
            p.GeneratedAt,
            p.GeneratedBy?.Fullname,
            linkedBudgetPlans,
            [.. matchingItems.Select(i => new PurchaseOrderItemResponse(
                i.Id,
                i.BudgetPlanItemId,
                i.ItemShadowId,
                i.ItemCode,
                i.ItemName,
                i.CoaCode,
                i.CoaName,
                i.VendorShadowId,
                i.VendorCode,
                i.VendorName,
                i.UomMasterId,
                i.UomCode,
                i.UomName,
                i.IsRfba,
                i.BillOfLading,
                i.CostValue,
                i.Quantity,
                i.TotalValue,
                i.SortOrder,
                i.PpnTaxTypeCode,
                i.PpnRate,
                i.PphTaxTypeCode,
                i.PphRate,
                i.PpnAmount,
                i.PphAmount,
                i.GrandTotal,
                i.CostTreatment))],
            matchingItems.Sum(i => i.GrandTotal),
            matchingItems.Count
        );
    }
}
