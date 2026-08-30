namespace WAMS.Infrastructure.ExternalSync.Spk;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Spk;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class SpkSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<SpkSyncService> logger)
    : BaseSyncService<SpkErpDto, SpkShadow>(dbFactory, logger)
{
    public override string ServiceName => "SpkSync";

    protected override async Task<List<SpkErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetSpksAsync(companyCode, ct);

    protected override void ValidateSchema(SpkErpDto dto)
    {
        // "BL" rows are bills-of-lading with no matching MO/LO document yet - ERP sends them
        // with every order-specific field blank. BlNo is the only field guaranteed populated.
        if (dto.Type == "BL")
        {
            if (string.IsNullOrWhiteSpace(dto.BlNo))
                throw new SyncSchemaException(
                    ErrorMessages.Sync.MissingRequiredField(nameof(dto.BlNo), JsonSerializer.Serialize(dto)));
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.DocNo))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.DocNo), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.ItemCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.ItemCode), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<SpkShadow>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.SpkShadows
            .IgnoreQueryFilters()
            .Where(s => s.CompanyId == companyId)
            .ToListAsync(ct);

    // BL rows all share blank DocNo/ItemCode - keying strictly by Type keeps them distinct via BlNo
    // instead of collapsing hundreds of rows into one under the (DocNo, ItemCode) key.
    // A single MO/LO doc can also carry the same ItemCode split across multiple BLs (partial
    // shipments) - fold ItemCode+BlNo into one composite string component so the existing
    // 2-string StringTupleComparer keeps those lines distinct too, instead of collapsing them.
    private static (string, string) KeyOfDto(SpkErpDto d)
        => d.Type == "BL" ? ("BL", d.BlNo ?? "") : (d.DocNo, d.ItemCode + "|" + d.BlNo);

    private static (string, string) KeyOfEntity(SpkShadow s)
        => s.Type == "BL" ? ("BL", s.BlNo ?? "") : (s.DocNo, s.ItemCode + "|" + s.BlNo);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<SpkShadow> existing,
        List<SpkErpDto> incoming,
        DateTime now)
    {
        // ERP can return duplicate rows (same DocNo+ItemCode, or same BlNo). last-wins dedup
        var dedupedIncoming = incoming
            .GroupBy(KeyOfDto, StringTupleComparer.Instance)
            .Select(g => g.Last())
            .ToList();

        var existingByKey = existing
            .GroupBy(KeyOfEntity, StringTupleComparer.Instance)
            .ToDictionary(g => g.Key, g => g.First(), StringTupleComparer.Instance);

        var erpKeys = dedupedIncoming
            .Select(KeyOfDto)
            .ToHashSet(StringTupleComparer.Instance);

        int added = 0, updated = 0, deactivated = 0;

        foreach (var dto in dedupedIncoming)
        {
            if (existingByKey.TryGetValue(KeyOfDto(dto), out var row))
            {
                var changed =
                    row.Type != dto.Type || row.BaseDoc != dto.BaseDoc ||
                    row.BaseDocNo != dto.BaseDocNo || row.CardCode != dto.CardCode ||
                    row.CardName != dto.CardName || row.ItemName != dto.ItemName ||
                    row.Quantity != dto.Quantity || row.DeliveryQty != dto.DeliveryQty ||
                    row.UoM != dto.UoM || row.PackType != dto.PackType ||
                    row.WhsCode != dto.WhsCode || row.WhsName != dto.WhsName ||
                    row.DocStatus != dto.DocStatus || row.BlNo != dto.BlNo || !row.IsActive;

                if (changed)
                {
                    row.Type = dto.Type;
                    row.BaseDoc = dto.BaseDoc;
                    row.BaseDocNo = dto.BaseDocNo;
                    row.CardCode = dto.CardCode;
                    row.CardName = dto.CardName;
                    row.ItemName = dto.ItemName;
                    row.Quantity = dto.Quantity;
                    row.DeliveryQty = dto.DeliveryQty;
                    row.UoM = dto.UoM;
                    row.PackType = dto.PackType;
                    row.WhsCode = dto.WhsCode;
                    row.WhsName = dto.WhsName;
                    row.DocStatus = dto.DocStatus;
                    row.BlNo = dto.BlNo;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.SpkShadows.Add(new SpkShadow
                {
                    CompanyId = companyId,
                    Type = dto.Type,
                    DocNo = dto.DocNo,
                    BaseDoc = dto.BaseDoc,
                    BaseDocNo = dto.BaseDocNo,
                    CardCode = dto.CardCode,
                    CardName = dto.CardName,
                    ItemCode = dto.ItemCode,
                    ItemName = dto.ItemName,
                    Quantity = dto.Quantity,
                    DeliveryQty = dto.DeliveryQty,
                    UoM = dto.UoM,
                    PackType = dto.PackType,
                    WhsCode = dto.WhsCode,
                    WhsName = dto.WhsName,
                    DocStatus = dto.DocStatus,
                    BlNo = dto.BlNo,
                    IsActive = true,
                    FirstSeenAt = now,
                    SyncedAt = now,
                });
                added++;
            }
        }

        foreach (var stale in existing.Where(s => s.IsActive && !erpKeys.Contains(KeyOfEntity(s))))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;
            logger.LogInformation(
                "[{Service}] Deactivating SPK {DocNo} for company={CompanyId}",
                ServiceName,
                stale.DocNo,
                companyId);
        }

        return (added, updated, deactivated);
    }
}
