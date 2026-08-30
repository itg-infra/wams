namespace WAMS.Infrastructure.ExternalSync.TransportOrder;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;
using WAMS.Infrastructure.ExternalSync.ErpHttpClient;

public class ToSyncService(
    ErpApiClient erp,
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<ToSyncService> logger)
    : BaseSyncService<ToErpDto, TransportOrderShadow>(dbFactory, logger)
{
    public override string ServiceName => "ToSync";

    protected override async Task<List<ToErpDto>?> FetchAsync(string companyCode, CancellationToken ct)
        => await erp.GetTransportOrdersAsync(companyCode, ct);

    protected override void ValidateSchema(ToErpDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DocNo))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.DocNo), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.ItemCode))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.ItemCode), JsonSerializer.Serialize(dto)));

        if (string.IsNullOrWhiteSpace(dto.BlNo))
            throw new SyncSchemaException(
                ErrorMessages.Sync.MissingRequiredField(nameof(dto.BlNo), JsonSerializer.Serialize(dto)));
    }

    protected override async Task<List<TransportOrderShadow>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct)
        => await db.TransportOrderShadows
            .IgnoreQueryFilters()
            .Where(t => t.CompanyId == companyId)
            .ToListAsync(ct);

    // Same (DocNo, BlNo) legitimately repeats across rows that differ only by VehicleNo
    // (one shipment split across multiple trucks). Fold BlNo+VehicleNo into one composite
    // string component so the existing 2-string StringTupleComparer keeps them distinct,
    // instead of introducing a 3-string tuple comparer for this one call site.
    private static (string, string) KeyOfDto(ToErpDto d)
        => (d.DocNo, d.BlNo + "|" + d.VehiclePlate);

    private static (string, string) KeyOfEntity(TransportOrderShadow t)
        => (t.DocNo, t.BlNo + "|" + t.VehicleNo);

    protected override (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<TransportOrderShadow> existing,
        List<ToErpDto> incoming,
        DateTime now)
    {
        // ERP can return duplicate rows for the same (DocNo, BlNo, VehiclePlate) - last-wins dedup
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
                    row.Type != dto.Type || row.CardCode != dto.CardCode ||
                    row.CardName != dto.CardName || row.VehicleNo != dto.VehiclePlate ||
                    row.VehicleType != dto.VehicleType || row.ItemCode != dto.ItemCode ||
                    row.ItemName != dto.ItemName ||
                    row.Quantity != dto.Quantity || row.UoM != dto.UoM ||
                    row.WhsCode != dto.WhsCode || row.WhsName != dto.WhsName ||
                    row.DocStatus != dto.DocStatus || !row.IsActive;

                if (changed)
                {
                    row.Type = dto.Type;
                    row.CardCode = dto.CardCode;
                    row.CardName = dto.CardName;
                    row.VehicleNo = dto.VehiclePlate;
                    row.VehicleType = dto.VehicleType;
                    row.ItemCode = dto.ItemCode;
                    row.ItemName = dto.ItemName;
                    row.Quantity = dto.Quantity;
                    row.UoM = dto.UoM;
                    row.WhsCode = dto.WhsCode;
                    row.WhsName = dto.WhsName;
                    row.DocStatus = dto.DocStatus;
                    row.IsActive = true;
                    row.SyncedAt = now;
                    updated++;
                }
            }
            else
            {
                db.TransportOrderShadows.Add(new TransportOrderShadow
                {
                    CompanyId = companyId,
                    DocNo = dto.DocNo,
                    Type = dto.Type,
                    CardCode = dto.CardCode,
                    CardName = dto.CardName,
                    VehicleNo = dto.VehiclePlate,
                    VehicleType = dto.VehicleType,
                    BlNo = dto.BlNo,
                    ItemCode = dto.ItemCode,
                    ItemName = dto.ItemName,
                    Quantity = dto.Quantity,
                    UoM = dto.UoM,
                    WhsCode = dto.WhsCode,
                    WhsName = dto.WhsName,
                    DocStatus = dto.DocStatus,
                    IsActive = true,
                    FirstSeenAt = now,
                    SyncedAt = now,
                });
                added++;
            }
        }

        foreach (var stale in existing.Where(
            t => t.IsActive && !erpKeys.Contains(KeyOfEntity(t), StringTupleComparer.Instance)))
        {
            stale.IsActive = false;
            stale.SyncedAt = now;
            deactivated++;
        }

        return (added, updated, deactivated);
    }
}
