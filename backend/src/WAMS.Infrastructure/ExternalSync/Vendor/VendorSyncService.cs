namespace WAMS.Infrastructure.ExternalSync.Vendor;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Vendors;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class VendorSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<VendorSyncService> logger)
    : BaseSyncService<VendorErpDto, VendorShadow>(dbFactory, logger)
{
    public override string ServiceName => "VendorSync";

    protected override async Task<List<VendorErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetVendorsAsync(companyCode, ct);

    protected override void ValidateSchema(VendorErpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CardCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.CardCode), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.CardName))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.CardName), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<VendorShadow>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.VendorShadows
            .IgnoreQueryFilters()
            .Where(v => v.CompanyId == companyId)
            .ToListAsync(ct);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<VendorShadow> existing,
        List<VendorErpDto> incoming,
        DateTime now)
    {
        var existingByCode = existing.ToDictionary(v => v.CardCode, StringComparer.OrdinalIgnoreCase);
        int added = 0, updated = 0, deactivated = 0;

        foreach (var dto in incoming)
        {
            if (existingByCode.TryGetValue(dto.CardCode, out var row))
            {
                if (row.CardName != dto.CardName || !row.IsActive)
                {
                    row.CardName = dto.CardName;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.VendorShadows.Add(new VendorShadow
                {
                    CompanyId = companyId,
                    CardCode = dto.CardCode,
                    CardName = dto.CardName,
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                });
                added++;
            }
        }

        var erpCodes = new HashSet<string>(
            incoming.Select(v => v.CardCode),
            StringComparer.OrdinalIgnoreCase);

        foreach (var stale in existing.Where(v => v.IsActive && !erpCodes.Contains(v.CardCode)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;
            logger.LogInformation(
                "[{Service}] Deactivating vendor {Code} for company={CompanyId}",
                ServiceName,
                stale.CardCode,
                companyId);
        }

        return (added, updated, deactivated);
    }
}
