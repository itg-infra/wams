namespace WAMS.Infrastructure.ExternalSync.Warehouse;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Application.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class WarehouseSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<WarehouseSyncService> logger)
    : BaseSyncService<WarehouseErpDto, WarehouseShadow>(dbFactory, logger)
{
    public override string ServiceName => "WarehouseSync";

    protected override async Task<List<WarehouseErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetWarehousesAsync(companyCode, ct);

    protected override void ValidateSchema(WarehouseErpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.WhsCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.WhsCode), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.WhsName))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.WhsName), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<WarehouseShadow>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.WarehouseShadows
            .IgnoreQueryFilters()
            .Where(w => w.CompanyId == companyId)
            .ToListAsync(ct);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<WarehouseShadow> existing,
        List<WarehouseErpDto> incoming,
        DateTime now)
    {
        var existingByCode = existing.ToDictionary(w => w.Code, StringComparer.OrdinalIgnoreCase);

        // normalized province name/alias -> province id.
        // Keys are normalized on BOTH sides (here and in ResolveProvince) so lookups
        // match regardless of casing/whitespace in the stored name or alias.
        var provinceLookup = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var p in db.Provinces.Select(p => new { p.Id, p.Name }).ToList())
            provinceLookup[ProvinceNormalizer.Normalize(p.Name)] = p.Id;
        foreach (var a in db.ProvinceAliases.Select(a => new { a.Alias, a.ProvinceId }).ToList())
            provinceLookup[ProvinceNormalizer.Normalize(a.Alias)] = a.ProvinceId;

        long? ResolveProvince(string? location)
        {
            var key = ProvinceNormalizer.Normalize(location);
            if (key.Length == 0) return null;
            if (provinceLookup.TryGetValue(key, out var id)) return id;

            logger.LogWarning(
                "[{Service}] Unmapped warehouse location '{Location}' (company={CompanyId}) - province_id left null",
                ServiceName,
                location,
                companyId);

            return null;
        }

        int added = 0, updated = 0, deactivated = 0;

        foreach (var dto in incoming)
        {
            if (existingByCode.TryGetValue(dto.WhsCode, out var row))
            {
                var resolvedProvinceId = ResolveProvince(dto.Location);
                if (row.Name != dto.WhsName || row.Location != dto.Location
                    || row.ProvinceId != resolvedProvinceId || !row.IsActive)
                {
                    row.Name = dto.WhsName;
                    row.Location = dto.Location;
                    row.ProvinceId = resolvedProvinceId;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.WarehouseShadows.Add(new WarehouseShadow
                {
                    CompanyId = companyId,
                    Code = dto.WhsCode,
                    Name = dto.WhsName,
                    Location = dto.Location,
                    ProvinceId = ResolveProvince(dto.Location),
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                });
                added++;
            }
        }

        var erpCodes = new HashSet<string>(
            incoming.Select(w => w.WhsCode),
            StringComparer.OrdinalIgnoreCase);

        foreach (var stale in existing.Where(w => w.IsActive && !erpCodes.Contains(w.Code)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;

            logger.LogInformation(
                "[{Service}] Deactivating warehouse {Code} for company={CompanyId}",
                ServiceName,
                stale.Code,
                companyId);
        }

        return (added, updated, deactivated);
    }
}
