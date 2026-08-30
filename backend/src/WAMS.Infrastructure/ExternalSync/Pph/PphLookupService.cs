namespace WAMS.Infrastructure.ExternalSync.Pph;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Companies;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.Caching.Common;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class PphLookupService(
    AppDbContext db,
    ErpApiClient erp,
    IVendorShadowRepository vendorRepo,
    ICompanyRepository companyRepo,
    HybridCache cache,
    ICacheInvalidationService cacheInvalidator,
    ILogger<PphLookupService> logger) : IPphLookupService
{
    public async Task<List<TaxTypeResponse>> GetOrRefreshAsync(
        long vendorShadowId,
        CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(vendorShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Vendor.NotFound(vendorShadowId));

        var company = await companyRepo.GetByIdAsync(vendor.CompanyId, ct)
            ?? throw new NotFoundException(ErrorMessages.Company.NotFound(vendor.CompanyId));

        var persisted = await db.VendorPphAssignments
            .Include(a => a.TaxType)
            .Where(a => a.VendorShadowId == vendorShadowId && a.IsActive)
            .ToListAsync(ct);

        var fresh = await erp.GetPphAsync(company.Code, vendor.CardCode, ct);

        if (fresh is null)
        {
            logger.LogWarning(
                "[PphLookupService] SAP call failed for vendor={CardCode}, falling back to {Count} persisted row(s)",
                vendor.CardCode,
                persisted.Count);
            return persisted.Select(a => TaxTypeResponse.From(a.TaxType)).ToList();
        }

        var now = DateTime.UtcNow;

        var existingPphTaxTypes = await db.TaxTypes
            .Where(t => t.CompanyId == vendor.CompanyId && t.Category == TaxCategory.Pph)
            .ToListAsync(ct);
        var taxTypeByCode = existingPphTaxTypes.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in fresh)
        {
            if (!taxTypeByCode.TryGetValue(dto.WtCode, out var taxType))
            {
                taxType = new TaxType
                {
                    CompanyId = vendor.CompanyId,
                    Category = TaxCategory.Pph,
                    Code = dto.WtCode,
                    Name = dto.WtName,
                    Rate = dto.Rate,
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                };
                db.TaxTypes.Add(taxType);
                taxTypeByCode[dto.WtCode] = taxType;
            }
            else if (taxType.Name != dto.WtName || taxType.Rate != dto.Rate || !taxType.IsActive)
            {
                taxType.Name = dto.WtName;
                taxType.Rate = dto.Rate;
                taxType.IsActive = true;
                taxType.UpdatedAt = now;
            }
        }

        // First save: persist any newly-added TaxType rows so they get real (non-zero) database
        // ids before VendorPphAssignment rows below try to reference them by TaxTypeId.
        await db.SaveChangesAsync(ct);

        var freshTaxTypeIds = fresh.Select(dto => taxTypeByCode[dto.WtCode].Id).ToHashSet();
        var assignmentByTaxTypeId = persisted.ToDictionary(a => a.TaxTypeId);

        foreach (var taxTypeId in freshTaxTypeIds)
        {
            if (assignmentByTaxTypeId.TryGetValue(taxTypeId, out var existingAssignment))
            {
                if (!existingAssignment.IsActive)
                {
                    existingAssignment.IsActive = true;
                    existingAssignment.SyncedAt = now;
                    existingAssignment.UpdatedAt = now;
                }
            }
            else
            {
                db.VendorPphAssignments.Add(new VendorPphAssignment
                {
                    VendorShadowId = vendorShadowId,
                    TaxTypeId = taxTypeId,
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                });
            }
        }

        foreach (var stale in persisted.Where(a => !freshTaxTypeIds.Contains(a.TaxTypeId)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            stale.UpdatedAt = now;
            logger.LogInformation(
                "[PphLookupService] Deactivating WT assignment {Code} for vendor={CardCode}",
                stale.TaxType.Code,
                vendor.CardCode);
        }

        await db.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(CacheTags.TaxTypes, ct);
        await cacheInvalidator.InvalidateRateCardsAsync(ct);

        return fresh.Select(dto => TaxTypeResponse.From(taxTypeByCode[dto.WtCode])).ToList();
    }
}
