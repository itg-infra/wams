namespace WAMS.Infrastructure.ExternalSync.Ppn;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class PpnSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<PpnSyncService> logger)
    : BaseSyncService<PpnErpDto, TaxType>(dbFactory, logger)
{
    public override string ServiceName => "PpnSync";

    protected override async Task<List<PpnErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetPpnAsync(companyCode, ct);

    protected override void ValidateSchema(PpnErpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PpnCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.PpnCode), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.PpnName))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.PpnName), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<TaxType>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.TaxTypes
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId && t.Category == TaxCategory.Ppn)
            .ToListAsync(ct);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<TaxType> existing,
        List<PpnErpDto> incoming,
        DateTime now)
    {
        var existingByCode = existing.ToDictionary(t => t.Code, StringComparer.OrdinalIgnoreCase);
        int added = 0, updated = 0, deactivated = 0;

        foreach (var dto in incoming)
        {
            if (existingByCode.TryGetValue(dto.PpnCode, out var row))
            {
                if (row.Name != dto.PpnName || row.Rate != dto.Rate || !row.IsActive)
                {
                    row.Name = dto.PpnName;
                    row.Rate = dto.Rate;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.TaxTypes.Add(new TaxType
                {
                    CompanyId = companyId,
                    Category = TaxCategory.Ppn,
                    Code = dto.PpnCode,
                    Name = dto.PpnName,
                    Rate = dto.Rate,
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                });
                added++;
            }
        }

        var erpCodes = new HashSet<string>(
            incoming.Select(t => t.PpnCode),
            StringComparer.OrdinalIgnoreCase);

        foreach (var stale in existing.Where(t => t.IsActive && !erpCodes.Contains(t.Code)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;
            logger.LogInformation(
                "[{Service}] Deactivating PPn code {Code} for company={CompanyId}",
                ServiceName,
                stale.Code,
                companyId);
        }

        return (added, updated, deactivated);
    }
}
