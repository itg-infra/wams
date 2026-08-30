namespace WAMS.Infrastructure.ExternalSync.Item;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Items;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class ItemSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<ItemSyncService> logger)
    : BaseSyncService<ItemErpDto, ItemShadow>(dbFactory, logger)
{
    public override string ServiceName => "ItemSync";

    protected override async Task<List<ItemErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetItemsAsync(companyCode, ct);

    protected override void ValidateSchema(ItemErpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.ItemCode), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.ItemName))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.ItemName), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<ItemShadow>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.ItemShadows
            .IgnoreQueryFilters()
            .Where(i => i.CompanyId == companyId)
            .ToListAsync(ct);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<ItemShadow> existing,
        List<ItemErpDto> incoming,
        DateTime now)
    {
        var existingByCode = existing.ToDictionary(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);
        int added = 0, updated = 0, deactivated = 0;

        foreach (var dto in incoming)
        {
            if (existingByCode.TryGetValue(dto.ItemCode, out var row))
            {
                if (row.ItemName != dto.ItemName || row.AcctCode != dto.AcctCode ||
                    row.AcctName != dto.AcctName || !row.IsActive)
                {
                    row.ItemName = dto.ItemName;
                    row.AcctCode = dto.AcctCode;
                    row.AcctName = dto.AcctName;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.ItemShadows.Add(new ItemShadow
                {
                    CompanyId = companyId,
                    ItemCode = dto.ItemCode,
                    ItemName = dto.ItemName,
                    AcctCode = dto.AcctCode,
                    AcctName = dto.AcctName,
                    IsActive = true,
                    SyncedAt = now,
                    FirstSeenAt = now,
                });
                added++;
            }
        }

        var erpCodes = new HashSet<string>(
            incoming.Select(i => i.ItemCode),
            StringComparer.OrdinalIgnoreCase);

        foreach (var stale in existing.Where(i => i.IsActive && !erpCodes.Contains(i.ItemCode)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;

            logger.LogInformation(
                "[{Service}] Deactivating item {Code} for company={CompanyId}",
                ServiceName,
                stale.ItemCode,
                companyId);
        }

        return (added, updated, deactivated);
    }
}
