namespace WAMS.Application.Services.RateCards;

using FluentValidation;
using Microsoft.Extensions.Configuration;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Items;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using DomainValidationException = Domain.Exceptions.ValidationException;

public class RateCardService(
    IRateCardRepository rateCardRepo,
    IVendorShadowRepository vendorRepo,
    IItemShadowRepository itemRepo,
    IUomMasterRepository uomRepo,
    ITaxTypeRepository taxTypeRepo,
    IConfiguration configuration,
    IUnitOfWork uow,
    IValidator<CreateRateCardRequest> createValidator,
    IValidator<UpdateRateCardRequest> updateValidator
) : IRateCardService
{
    public async Task<PaginatedResponse<RateCardSummaryResponse>> GetAllAsync(
        RateCardStatus? status,
        long? vendorId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await rateCardRepo.GetAllAsync(status, vendorId, query, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return new PaginatedResponse<RateCardSummaryResponse>(true, items, meta);
    }

    public IAsyncEnumerable<RateCardSummaryResponse> StreamAllAsync(
        RateCardStatus? status,
        long? vendorId,
        DataTableQuery query,
        int limit,
        CancellationToken ct = default
    )
        => rateCardRepo.StreamAllAsync(status, vendorId, query, limit, ct);

    public async Task<RateCardResponse> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var rc = await rateCardRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.RateCard.NotFound(id));

        return MapDetail(rc);
    }

    public async Task<RateCardResponse> CreateAsync(
        long userId,
        CreateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.First().ErrorMessage);

        _ = await vendorRepo.GetByIdAsync(request.VendorShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(request.VendorShadowId));

        var taxTypeMap = await ValidateItemsAsync(request.Items, enforceActive: true, ct);
        var defaultPpn = await GetDefaultPpnIfNeededAsync(request.Items, ct);

        var rc = new RateCard
        {
            VendorShadowId = request.VendorShadowId,
            Status = RateCardStatus.Draft,
            CreatedByUserId = userId,
        };

        foreach (var itemReq in request.Items)
            rc.Items.Add(BuildItem(itemReq, taxTypeMap, defaultPpn));

        await rateCardRepo.CreateAsync(rc, ct);
        await uow.CommitAsync(ct);

        var created = await rateCardRepo.GetByIdWithItemsAsync(rc.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.RateCard.NotFoundAfterCreation);

        return MapDetail(created);
    }

    public async Task<RateCardResponse> CreateAndSubmitAsync(
        long userId,
        CreateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.First().ErrorMessage);

        _ = await vendorRepo.GetByIdAsync(request.VendorShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(request.VendorShadowId));

        if (request.Items.Count == 0)
            throw new DomainValidationException(ErrorMessages.RateCard.MustHaveItemBeforeSubmit);

        var taxTypeMap = await ValidateItemsAsync(request.Items, enforceActive: true, ct);
        var defaultPpn = await GetDefaultPpnIfNeededAsync(request.Items, ct);

        var rc = new RateCard
        {
            VendorShadowId = request.VendorShadowId,
            Status = RateCardStatus.Submitted,
            CreatedByUserId = userId,
            SubmittedAt = DateTime.UtcNow,
        };

        foreach (var itemReq in request.Items)
            rc.Items.Add(BuildItem(itemReq, taxTypeMap, defaultPpn));

        await rateCardRepo.CreateAsync(rc, ct);
        await uow.CommitAsync(ct);

        var created = await rateCardRepo.GetByIdWithItemsAsync(rc.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.RateCard.NotFoundAfterCreation);

        return MapDetail(created);
    }

    public async Task<RateCardResponse> UpdateAsync(
        long id,
        UpdateRateCardRequest request,
        CancellationToken ct = default
    )
    {
        var validation = await updateValidator.ValidateAsync(request, ct);

        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.First().ErrorMessage);

        var rc = await rateCardRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.RateCard.NotFound(id));

        if (request.VendorShadowId.HasValue)
        {
            var vendor = await vendorRepo.GetByIdAsync(request.VendorShadowId.Value, ct)
                ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(request.VendorShadowId.Value));
            rc.VendorShadowId = request.VendorShadowId.Value;
        }

        var taxTypeMap = await ValidateItemsAsync(request.Items, enforceActive: false, ct);
        var defaultPpn = await GetDefaultPpnIfNeededAsync(request.Items, ct);

        // Replace items strategy - also re-snapshots each item's tax rate from the current TaxType.
        rc.Items.Clear();
        foreach (var itemReq in request.Items)
            rc.Items.Add(BuildItem(itemReq, taxTypeMap, defaultPpn));

        rc.UpdatedAt = DateTime.UtcNow;
        await rateCardRepo.UpdateAsync(rc, ct);
        await uow.CommitAsync(ct);

        var updated = await rateCardRepo.GetByIdWithItemsAsync(id, ct)!;

        return MapDetail(updated!);
    }

    public async Task<RateCardResponse> SubmitAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var rc = await rateCardRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.RateCard.NotFound(id));

        if (!rc.Status.CanBeSubmitted)
            throw new ForbiddenException(ErrorMessages.RateCard.CannotSubmitOnlyDraft);

        if (!rc.Items.Any())
            throw new DomainValidationException(ErrorMessages.RateCard.MustHaveItemBeforeSubmit);

        rc.Status = RateCardStatus.Submitted;
        rc.SubmittedAt = DateTime.UtcNow;
        rc.UpdatedAt = DateTime.UtcNow;

        await rateCardRepo.UpdateAsync(rc, ct);
        await uow.CommitAsync(ct);

        var submitted = await rateCardRepo.GetByIdWithItemsAsync(id, ct)!;

        return MapDetail(submitted!);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        if (!await rateCardRepo.ExistsAsync(id, ct))
            throw new NotFoundException(ErrorMessages.RateCard.NotFound(id));

        await rateCardRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);
    }

    private async Task<Dictionary<long, TaxType>> ValidateItemsAsync(
        IEnumerable<CreateRateCardItemRequest> items,
        bool enforceActive,
        CancellationToken ct
    )
    {
        var itemList = items.ToList();
        var itemIds = itemList.Select(i => i.ItemShadowId).Distinct().ToList();
        var uomIds = itemList.Select(i => i.UomMasterId).Distinct().ToList();
        var taxTypeIds = itemList
            .SelectMany(i => new[] { i.PpnTaxTypeId, i.PphTaxTypeId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Sequential lookups - all three share the same scoped DbContext, which cannot run
        // concurrent operations (EF Core throws InvalidOperationException if it's attempted).
        var foundItems = await itemRepo.GetByIdsAsync(itemIds, ct);
        var foundUoms = await uomRepo.GetByIdsAsync(uomIds, ct);
        var foundTaxTypes = taxTypeIds.Count > 0
            ? await taxTypeRepo.GetByIdsAsync(taxTypeIds, ct)
            : [];

        var foundItemIds = foundItems.Select(i => i.Id).ToHashSet();
        var missingItemIds = itemIds.Where(id => !foundItemIds.Contains(id)).ToList();
        if (missingItemIds.Count > 0)
            throw new NotFoundException(ErrorMessages.RateCard.ItemNotFound(missingItemIds[0]));

        var foundUomIds = foundUoms.Select(u => u.Id).ToHashSet();
        var missingUomIds = uomIds.Where(id => !foundUomIds.Contains(id)).ToList();
        if (missingUomIds.Count > 0)
            throw new NotFoundException(ErrorMessages.RateCard.UomNotFound(missingUomIds[0]));

        if (taxTypeIds.Count == 0)
            return [];

        var taxTypeMap = foundTaxTypes.ToDictionary(t => t.Id);

        foreach (var itemReq in itemList)
        {
            ValidateTaxTypeRef(itemReq.PpnTaxTypeId, TaxCategory.Ppn, taxTypeMap, enforceActive);
            ValidateTaxTypeRef(itemReq.PphTaxTypeId, TaxCategory.Pph, taxTypeMap, enforceActive);
        }

        return taxTypeMap;
    }

    // PPN falls back to defaultPpn when unset (SAP requires a TaxCode); PPh has no such requirement.
    private static RateCardItem BuildItem(
        CreateRateCardItemRequest itemReq,
        Dictionary<long, TaxType> taxTypeMap,
        TaxType? defaultPpn
    )
    {
        var ppn = itemReq.PpnTaxTypeId is { } ppnId ? taxTypeMap[ppnId] : defaultPpn;
        return new()
        {
            ItemShadowId = itemReq.ItemShadowId,
            UomMasterId = itemReq.UomMasterId,
            CostValue = itemReq.CostValue,
            PpnTaxTypeId = ppn?.Id,
            PpnTaxTypeCode = ppn?.Code,
            PpnRate = ppn?.Rate,
            PphTaxTypeId = itemReq.PphTaxTypeId,
            PphTaxTypeCode = itemReq.PphTaxTypeId is { } pphCodeId ? taxTypeMap[pphCodeId].Code : null,
            PphRate = itemReq.PphTaxTypeId is { } pphRateId ? taxTypeMap[pphRateId].Rate : null,
            CostTreatment = itemReq.CostTreatment,
        };
    }

    private async Task<TaxType?> GetDefaultPpnIfNeededAsync(
        IEnumerable<CreateRateCardItemRequest> items,
        CancellationToken ct
    )
    {
        if (items.All(i => i.PpnTaxTypeId.HasValue))
            return null;

        var code = configuration["ErpApi:SapDefaultTaxCode"] ?? "PPNin0";

        return await taxTypeRepo.GetByCodeAsync(TaxCategory.Ppn, code, ct)
            ?? throw new InvalidOperationException(
                $"Default PPN tax type '{code}' not found - has the PPN sync run yet?");
    }

    private static void ValidateTaxTypeRef(
        long? taxTypeId,
        TaxCategory expectedCategory,
        Dictionary<long, TaxType> taxTypeMap,
        bool enforceActive
    )
    {
        if (!taxTypeId.HasValue)
            return;

        if (!taxTypeMap.TryGetValue(taxTypeId.Value, out var taxType))
            throw new NotFoundException(ErrorMessages.TaxType.NotFound(taxTypeId.Value));

        if (taxType.Category != expectedCategory)
            throw new DomainValidationException(ErrorMessages.TaxType.WrongCategory(taxTypeId.Value, expectedCategory.Value));

        if (enforceActive && !taxType.IsActive)
            throw new DomainValidationException(ErrorMessages.TaxType.Inactive(taxType.Code));
    }

    private static RateCardResponse MapDetail(RateCard r) => new(
        r.Id,
        new VendorSummaryResponse(r.Vendor.Id, r.Vendor.CardCode, r.Vendor.CardName),
        r.Status.ToString(),
        [.. r.Items.Select(MapItem)],
        r.CreatedAt,
        r.SubmittedAt
    );

    private static RateCardItemResponse MapItem(RateCardItem i) => new(
        i.Id,
        new ItemSummaryResponse(i.Item.Id, i.Item.ItemCode, i.Item.ItemName, i.Item.AcctCode, i.Item.AcctName),
        new UomResponse(i.Uom.Id, i.Uom.Code, i.Uom.Name, i.Uom.IsActive),
        i.CostValue,
        MapTax(i.PpnTaxTypeId, i.PpnTaxTypeCode, i.PpnRate),
        MapTax(i.PphTaxTypeId, i.PphTaxTypeCode, i.PphRate),
        i.CostTreatment
    );

    private static RateCardItemTaxResponse? MapTax(long? taxTypeId, string? code, decimal? rate) =>
        taxTypeId is { } id ? new RateCardItemTaxResponse(id, code!, rate ?? 0m) : null;
}
