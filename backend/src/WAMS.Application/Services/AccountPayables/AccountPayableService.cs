namespace WAMS.Application.Services.AccountPayables;

using WAMS.Application.Common;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class AccountPayableService(
    IAccountPayableRepository apRepo,
    IVendorShadowRepository vendorRepo,
    IPurchaseOrderRepository poRepo,
    ISapApiClient sapClient,
    IUnitOfWork uow,
    IWarehouseContext warehouseContext,
    IWarehouseShadowRepository warehouseRepo,
    IUserRepository userRepo,
    IRbacService rbacService,
    ICodeCounterRepository codeCounterRepo
) : IAccountPayableService
{
    public Task<(List<AccountPayableSummaryResponse> Items, int TotalCount)> GetAllAsync(
        AccountPayableQuery q,
        CancellationToken ct = default
    )
        => apRepo.GetAllAsync(q, ct);

    public IAsyncEnumerable<AccountPayableSummaryResponse> StreamAllAsync(
        AccountPayableQuery q,
        int limit,
        CancellationToken ct = default
    )
        => apRepo.StreamAllAsync(q, limit, ct);

    public async Task<AccountPayableResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var ap = await apRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(id));

        return MapDetail(ap);
    }

    public async Task<(List<ApprovedRecapApStatusResponse> Items, int Total)> GetApprovedRecapsAsync(
        long userId,
        int page,
        int limit,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await apRepo.GetApprovedRecapsWithApStatusAsync(
            warehouseIds?.ToArray(),
            page,
            limit,
            ct
        );
    }

    public async Task<List<AvailableApItemResponse>> GetAvailableItemsByBudgetPlansAsync(
        long userId,
        long vendorShadowId,
        List<long> budgetPlanIds,
        bool includeGenerated = false,
        long? excludeAccountPayableId = null,
        CancellationToken ct = default
    )
    {
        if (excludeAccountPayableId.HasValue)
        {
            var current = await apRepo.GetByIdWithItemsAsync(excludeAccountPayableId.Value, ct)
                ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(excludeAccountPayableId.Value));
            if (!current.Status.CanBeEdited)
                throw new ValidationException(ErrorMessages.AccountPayable.CannotUpdateOnlyDraft);
            if (current.VendorShadowId != vendorShadowId)
                throw new ValidationException(ErrorMessages.AccountPayable.ItemVendorMismatch(vendorShadowId));
        }

        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        return await apRepo.GetAvailableItemsByBudgetPlansAsync(
            vendorShadowId, budgetPlanIds, includeGenerated, excludeAccountPayableId, warehouseIds?.ToList(), ct);
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

    public async Task<AccountPayableTotalsResponse> PreviewAsync(
        long userId,
        PreviewAccountPayableRequest request,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        var availableItems = await apRepo.GetAvailableItemsAsync(
            request.VendorShadowId,
            request.Items,
            warehouseIds: warehouseIds?.ToList(),
            ct: ct
        );

        await ValidateAllItemsAvailableAsync(request.VendorShadowId, request.Items, availableItems, warehouseIds, ct);

        var scratchAp = new AccountPayable();
        BuildItems(scratchAp, request.Items, availableItems);

        var totals = AccountPayableTotalsCalculator.Compute(scratchAp.Items, request.DiscountAmount);

        var items = scratchAp.Items.OrderBy(i => i.SortOrder).Select(i => new AccountPayableItemResponse(
            i.Id,
            i.BudgetPlanItemId,
            i.BudgetPlanItem.BudgetPlanId,
            i.VendorShadowId,
            i.VendorCode,
            i.VendorName,
            i.ItemCode,
            i.ItemName,
            i.CoaCode,
            i.CoaName,
            i.UomCode,
            i.UomName,
            i.IsRfba,
            i.BillOfLading,
            i.UnitCost,
            i.UnitCount,
            i.BudgetPlanTotal,
            i.BudgetRealization,
            i.BudgetVariance,
            i.SortOrder,
            i.PpnTaxTypeCode,
            i.PpnRate,
            i.PphTaxTypeCode,
            i.PphRate,
            i.PpnAmount,
            i.PphAmount,
            i.GrandTotal,
            i.CostTreatment)).ToList();

        return new AccountPayableTotalsResponse(
            items,
            totals.DppTotal,
            totals.TotalPpnAmount,
            totals.TotalPphAmount,
            totals.TaxInclusiveGrandTotal,
            totals.DiscountAmount,
            totals.DiscountPercent,
            totals.TotalRealization,
            totals.TotalVariance
        );
    }

    public async Task<AccountPayableResponse> CreateAsync(
        long userId,
        CreateAccountPayableRequest request,
        CancellationToken ct = default
    )
    {
        if (request.Items.Count == 0)
            throw new ValidationException(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        var vendor = await vendorRepo.GetByIdAsync(request.VendorShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(request.VendorShadowId));

        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        AccountPayable ap = null!;

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            await apRepo.LockBudgetPlanItemsAsync(request.Items, innerCt);

            var availableItems = await apRepo.GetAvailableItemsAsync(
                request.VendorShadowId,
                request.Items,
                warehouseIds: warehouseIds?.ToList(),
                ct: innerCt
            );

            await ValidateAllItemsAvailableAsync(request.VendorShadowId, request.Items, availableItems, warehouseIds, innerCt);

            var prefix = $"AP-{DateTime.UtcNow:yyMM}";
            var code = await DocumentCodeGenerator.NextCodeAsync(codeCounterRepo, prefix, innerCt);

            ap = new AccountPayable
            {
                Code = code,
                VendorShadowId = request.VendorShadowId,
                Remark = request.Remark,
                DocDate = request.DocDate,
                Status = AccountPayableStatus.Draft,
                CreatedByUserId = userId,
                DiscountAmount = request.DiscountAmount,
            };

            BuildItems(ap, request.Items, availableItems);
            ValidateDiscountAmount(ap);

            await apRepo.CreateAsync(ap, innerCt);
            await uow.CommitAsync(innerCt);
        }, ct);

        // Populate navs from already-loaded data instead of a second DB round-trip.
        ap.Vendor = vendor;
        ap.CreatedBy = await userRepo.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException(ErrorMessages.User.NotFound(userId));
        ap.GeneratedBy = null;

        var warnings = await BuildPoWarningsAsync(ap.Items, ct);

        return MapDetail(ap, warnings);
    }

    public async Task<AccountPayableResponse> CreateAndGenerateAsync(
        long userId,
        CreateAccountPayableRequest request,
        CancellationToken ct = default
    )
    {
        var created = await CreateAsync(userId, request, ct);
        try
        {
            return await GenerateAsync(created.Id, userId, ct);
        }
        catch
        {
            var current = await apRepo.GetByIdWithItemsAsync(created.Id, CancellationToken.None);

            if (current?.SapApdpDocEntry is null)
                await apRepo.SoftDeleteAsync(created.Id, CancellationToken.None);

            throw;
        }
    }

    public async Task<AccountPayableResponse> UpdateAsync(
        long id,
        long userId,
        UpdateAccountPayableRequest request,
        CancellationToken ct = default
    )
    {
        var ap = await apRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(id));

        if (!ap.Status.CanBeEdited)
            throw new ValidationException(ErrorMessages.AccountPayable.CannotUpdateOnlyDraft);

        if (request.Items is not null && request.Items.Count == 0)
            throw new ValidationException(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);

        if (request.Remark is not null)
            ap.Remark = request.Remark;

        if (request.DocDate.HasValue)
            ap.DocDate = request.DocDate.Value;

        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            if (!await apRepo.LockForEditAsync(id, innerCt))
                throw new ConflictException(ErrorMessages.AccountPayable.GenerationInProgress(id));

            if (request.Items is not null)
            {
                await apRepo.LockBudgetPlanItemsAsync(request.Items, innerCt);

                var availableItems = await apRepo.GetAvailableItemsAsync(
                    ap.VendorShadowId,
                    request.Items,
                    excludeDocumentId: ap.Id,
                    warehouseIds: warehouseIds?.ToList(),
                    ct: innerCt
                );

                await ValidateAllItemsAvailableAsync(ap.VendorShadowId, request.Items, availableItems, warehouseIds, innerCt);

                ap.Items.Clear();
                BuildItems(ap, request.Items, availableItems);
            }

            if (request.DiscountAmount.HasValue)
                ap.DiscountAmount = request.DiscountAmount.Value;

            ValidateDiscountAmount(ap);

            ap.UpdatedAt = DateTime.UtcNow;

            // Entity tracked by EF's change tracker; no explicit UpdateAsync or re-fetch needed.
            await uow.CommitAsync(innerCt);
        }, ct);

        var warnings = await BuildPoWarningsAsync(ap.Items, ct);
        return MapDetail(ap, warnings);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var ap = await apRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(id));

        if (!ap.Status.CanBeDeleted)
            throw new ValidationException(ErrorMessages.AccountPayable.CannotDeleteOnlyDraft);

        if (!await apRepo.SoftDeleteAsync(id, ct))
            throw new ConflictException(ErrorMessages.AccountPayable.GenerationInProgress(id));

        await uow.CommitAsync(ct);
    }

    public async Task<AccountPayableResponse> GenerateAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var claimToken = Guid.NewGuid().ToString("N");
        if (!await apRepo.TryClaimForGenerationAsync(id, claimToken, ct))
        {
            var current = await apRepo.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(id));

            if (!current.Status.CanBeGenerated)
                throw new ValidationException(ErrorMessages.AccountPayable.CannotGenerateOnlyDraft);

            throw new ConflictException(ErrorMessages.AccountPayable.GenerationInProgress(id));
        }

        AccountPayable ap;

        try
        {
            ap = await apRepo.GetByIdWithItemsAsync(id, ct)
                ?? throw new NotFoundException(ErrorMessages.AccountPayable.NotFound(id));

            if (ap.Items.Count == 0)
                throw new ValidationException(ErrorMessages.AccountPayable.NoItemsCannotGenerate);

            await GenerateToSapAsync(ap, userId, claimToken, ct);
        }
        catch
        {
            // Compensating cleanup must run to completion even if the request's own ct
            // is already cancelled (e.g. client disconnect) otherwise this throws
            // OperationCanceledException before reaching `throw;`, replacing the original
            // exception and leaking the claim (never released, self-expires after the 15-minute lease).
            await apRepo.ReleaseGenerationClaimAsync(id, claimToken, CancellationToken.None);
            throw;
        }

        return MapDetail(ap);
    }

    private async Task GenerateToSapAsync(AccountPayable ap, long userId, string claimToken, CancellationToken ct)
    {
        var rfbaItems = ap.Items.Where(i => i.IsRfba).ToList();

        var totals = AccountPayableTotalsCalculator.Compute(ap.Items, ap.DiscountAmount);
        var discountPercent = totals.DiscountAmount > 0m ? totals.DiscountPercent : (decimal?)null;
        var poLineRefs = await poRepo.GetGeneratedPoLineRefsAsync(
            [.. ap.Items.Select(i => i.BudgetPlanItemId)],
            ct
        );

        var itemsWithoutPo = ap.Items
            .Where(i => !poLineRefs.ContainsKey(i.BudgetPlanItemId))
            .Select(i => i.BudgetPlanItemId)
            .ToList();

        if (itemsWithoutPo.Count > 0)
            throw new ValidationException(
                ErrorMessages.AccountPayable.ItemsMissingGeneratedPo(itemsWithoutPo));

        if (rfbaItems.Count > 0 && ap.SapApdpDocEntry is null)
        {
            var apdpRequest = new SapCreateApdpRequest(
                ap.Code,
                ap.Items.First().VendorCode,
                ap.DocDate,
                ap.Remark,
                [.. rfbaItems.Select(i => ToSapApLineItem(i, discountPercent, poLineRefs))]
            );

            var apdpResult = await sapClient.CreateApDownPaymentAsync(apdpRequest, ct)
                ?? throw new ValidationException(ErrorMessages.AccountPayable.SapNoApNumber);

            ap.SapApdpDocEntry = apdpResult.SapDocEntry;
            ap.UpdatedAt = DateTime.UtcNow;

            await uow.CommitAsync(ct);
        }

        // DrawAmount = RFBA total minus its proportional share of the whole-document discount.
        // PM applies the same discountPercent to both RFBA and non-RFBA lines when mixed.
        // When pure-RFBA (rfbaDpp == totals.DppTotal), this equals totals.TaxInclusiveGrandTotal.
        var rfbaDpp = rfbaItems.Sum(i => i.BudgetPlanTotal);
        var rfbaDiscountShare = totals.DppTotal == 0m ? 0m : ap.DiscountAmount * rfbaDpp / totals.DppTotal;
        var drawAmount = rfbaItems.Count == 0
            ? (decimal?)null
            : rfbaItems.Sum(i => i.GrandTotal) - rfbaDiscountShare;

        // Any RFBA presence suppresses WHT for the entire document
        var whTax = rfbaItems.Count > 0 ? null : BuildWhTaxLines(ap.Items);

        var invoiceRequest = new SapCreateApInvoiceRequest(
            ap.Code,
            ap.Items.First().VendorCode,
            ap.DocDate,
            ap.Remark,
            [.. ap.Items.Select(i => ToSapApLineItem(i, discountPercent, poLineRefs))],
            WhTax: whTax,
            ApdpDocEntry: rfbaItems.Count > 0 ? ap.SapApdpDocEntry : null,
            DrawAmount: drawAmount
        );

        var invoiceResult = await sapClient.CreateApInvoiceAsync(invoiceRequest, ct)
            ?? throw new ValidationException(ErrorMessages.AccountPayable.SapNoApNumber);

        var marked = await apRepo.MarkGeneratedAsync(
            ap.Id,
            claimToken,
            invoiceResult.SapApNumber,
            invoiceResult.SapDocEntry,
            ap.SapApdpDocEntry,
            userId,
            ct);

        if (!marked)
            throw new ConflictException(ErrorMessages.AccountPayable.GenerationInProgress(ap.Id));

        ap.Status = AccountPayableStatus.Generated;
        ap.SapApNumber = invoiceResult.SapApNumber;
        ap.SapDocEntry = invoiceResult.SapDocEntry;
        ap.GeneratedByUserId = userId;
        ap.GeneratedAt = DateTime.UtcNow;
        ap.GeneratedBy = await userRepo.GetByIdAsync(userId, ct);
    }

    private static List<SapWhTaxLine>? BuildWhTaxLines(IEnumerable<AccountPayableItem> items)
    {
        var lines = items
            .Where(i => i.PphTaxTypeCode is not null)
            .GroupBy(i => i.PphTaxTypeCode!)
            .Select(g => new SapWhTaxLine(g.Key, g.Sum(i => i.BudgetPlanTotal)))
            .ToList();

        return lines.Count == 0 ? null : lines;
    }

    private static SapApLineItem ToSapApLineItem(
        AccountPayableItem i,
        decimal? discountPercent,
        Dictionary<long, (int SapDocEntry, int LineIndex)> poLineRefs
    )
    {
        (int SapDocEntry, int LineIndex)? poRef = poLineRefs.TryGetValue(i.BudgetPlanItemId, out var r) ? r : null;

        return new(
            i.ItemCode,
            i.ItemName,
            i.CoaCode,
            i.UnitCount,
            i.UnitCost,
            i.UomCode,
            i.BudgetPlanTotal,
            i.BudgetRealization,
            i.PpnTaxTypeCode,
            i.PphTaxTypeCode,
            discountPercent,
            i.BudgetPlanItem.BudgetPlan.Warehouse.Code,
            i.BillOfLading,
            i.BudgetPlanItem.Spk?.ItemCode,
            poRef?.SapDocEntry,
            poRef?.LineIndex
        );
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

        var diagnostics = await apRepo.GetAvailabilityDiagnosticsAsync(vendorShadowId, missingIds, warehouseIds?.ToList(), ct);

        var reasons = diagnostics.Select(d => d switch
        {
            { Found: false } => ErrorMessages.AccountPayable.ItemNotFound(d.Id),
            { VendorMatches: false } => ErrorMessages.AccountPayable.ItemVendorMismatch(d.Id),
            { WarehouseInScope: false } => ErrorMessages.AccountPayable.ItemWarehouseNotAccessible(d.Id),
            { AlreadyGenerated: true } => ErrorMessages.AccountPayable.ItemAlreadyGenerated(d.Id),
            { RecapApproved: false } => ErrorMessages.AccountPayable.ItemRecapNotApproved(d.Id),
            { TakenByCode: not null } => ErrorMessages.AccountPayable.ItemAlreadyTaken(d.Id, d.TakenByCode),
            _ => ErrorMessages.AccountPayable.ItemUnavailable(d.Id),
        });

        throw new ValidationException(string.Join(" ", reasons));
    }

    private static void ValidateDiscountAmount(AccountPayable ap)
    {
        if (ap.DiscountAmount < 0m)
            throw new ValidationException(ErrorMessages.AccountPayable.DiscountNegative);

        var dppTotal = ap.Items.Sum(i => i.BudgetPlanTotal);

        if (ap.DiscountAmount > dppTotal)
            throw new ValidationException(ErrorMessages.AccountPayable.DiscountExceedsDpp(ap.DiscountAmount, dppTotal));
    }

    private static void BuildItems(
        AccountPayable ap,
        List<long> itemIds,
        List<BudgetPlanItem> availableItems
    )
    {
        var lookup = availableItems.ToDictionary(i => i.Id);
        for (var i = 0; i < itemIds.Count; i++)
        {
            var bpi = lookup[itemIds[i]];
            var tax = TaxCalculator.Calculate(bpi.TotalValue, bpi.PpnRate, bpi.PphRate);

            ap.Items.Add(new AccountPayableItem
            {
                BudgetPlanItemId = bpi.Id,
                BudgetPlanItem = bpi,
                VendorShadowId = bpi.VendorShadowId,
                VendorCode = bpi.Vendor.CardCode,
                VendorName = bpi.Vendor.CardName,
                ItemCode = bpi.Item.ItemCode,
                ItemName = bpi.Item.ItemName,
                CoaCode = bpi.Item.AcctCode,
                CoaName = bpi.Item.AcctName,
                UomCode = bpi.Uom.Code,
                UomName = bpi.Uom.Name,
                IsRfba = bpi.IsRfba,
                BillOfLading = bpi.BillOfLading,
                UnitCost = bpi.CostValue,
                UnitCount = bpi.Quantity,
                BudgetPlanTotal = bpi.TotalValue,
                BudgetRealization = 0m,
                BudgetVariance = bpi.TotalValue,
                SortOrder = i + 1,
                PpnTaxTypeCode = bpi.PpnTaxTypeCode,
                PpnRate = bpi.PpnRate,
                PphTaxTypeCode = bpi.PphTaxTypeCode,
                PphRate = bpi.PphRate,
                PpnAmount = tax.PpnAmount,
                PphAmount = tax.PphAmount,
                GrandTotal = tax.GrandTotal,
                CostTreatment = bpi.CostTreatment,
            });
        }
    }

    private async Task<List<string>?> BuildPoWarningsAsync(
        ICollection<AccountPayableItem> items, CancellationToken ct)
    {
        var bpiIds = items.Select(i => i.BudgetPlanItemId).ToList();
        var poLineRefs = await poRepo.GetGeneratedPoLineRefsAsync(bpiIds, ct);

        var missing = bpiIds.Where(id => !poLineRefs.ContainsKey(id)).ToList();
        if (missing.Count == 0) return null;

        return [ErrorMessages.AccountPayable.ItemsMissingGeneratedPo(missing)];
    }

    private static AccountPayableResponse MapDetail(AccountPayable ap, List<string>? warnings = null)
    {
        var budgetPlanCodes = ap.Items
            .Select(i => i.BudgetPlanItem.BudgetPlan.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        var linkedBudgetPlans = ap.Items
            .Select(i => new ApBudgetPlanLinkInfo(
                i.BudgetPlanItem.BudgetPlan.Id,
                i.BudgetPlanItem.BudgetPlan.Code))
            .DistinctBy(bp => bp.Id)
            .OrderBy(bp => bp.Code)
            .ToList();

        var totals = AccountPayableTotalsCalculator.Compute(ap.Items, ap.DiscountAmount);

        return new(
            ap.Id,
            ap.Code,
            ap.VendorShadowId,
            ap.Vendor.CardCode,
            ap.Vendor.CardName,
            ap.Status.Value,
            ap.DocDate,
            ap.Remark,
            ap.SapApNumber,
            budgetPlanCodes,
            linkedBudgetPlans,
            [.. ap.Items.OrderBy(i => i.SortOrder).Select(i => new AccountPayableItemResponse(
                i.Id,
                i.BudgetPlanItemId,
                i.BudgetPlanItem.BudgetPlanId,
                i.VendorShadowId,
                i.VendorCode,
                i.VendorName,
                i.ItemCode,
                i.ItemName,
                i.CoaCode,
                i.CoaName,
                i.UomCode,
                i.UomName,
                i.IsRfba,
                i.BillOfLading,
                i.UnitCost,
                i.UnitCount,
                i.BudgetPlanTotal,
                i.BudgetRealization,
                i.BudgetVariance,
                i.SortOrder,
                i.PpnTaxTypeCode,
                i.PpnRate,
                i.PphTaxTypeCode,
                i.PphRate,
                i.PpnAmount,
                i.PphAmount,
                i.GrandTotal,
                i.CostTreatment))
            ],
            totals.DppTotal,
            totals.TotalPpnAmount,
            totals.TotalPphAmount,
            totals.TaxInclusiveGrandTotal,
            ap.CreatedAt,
            ap.CreatedBy.Fullname,
            ap.GeneratedAt,
            ap.GeneratedBy?.Fullname,
            totals.DiscountAmount,
            totals.DiscountPercent,
            totals.TotalRealization,
            totals.TotalVariance,
            warnings);
    }
}
