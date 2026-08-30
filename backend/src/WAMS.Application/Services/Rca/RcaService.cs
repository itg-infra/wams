namespace WAMS.Application.Services.Rca;

using WAMS.Application.DTOs.Rca;
using WAMS.Application.Export;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Rca;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;

public class RcaService(
    IRcaRepository repo,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IRbacService rbacService,
    IPdfMetadataResolver metadataResolver,
    ITenantContext tenantContext
) : IRcaService
{
    public async Task<RcaDocument> GetDocumentAsync(
        RcaQuery query,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        var companyId = tenantContext.IsSet ? tenantContext.CompanyId : null;

        var dateFrom = query.DateFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dateTo = query.DateTo.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var data = await repo.GetDataAsync(query.WarehouseCode, dateFrom, dateTo, warehouseIds, companyId, ct);
        var metadata = await metadataResolver.ResolveAsync("Rekapitulasi Kas Operasional (RCA)", ct);

        var rcaId = GenerateRcaId(metadata.CompanyCode, query.WarehouseCode, query.DateTo);

        return new RcaDocument(
            RcaId: rcaId,
            CompanyName: metadata.CompanyName,
            LogoData: metadata.LogoData,
            WarehouseCode: query.WarehouseCode,
            Area: data.WarehouseLocation,
            DateFrom: query.DateFrom,
            DateTo: query.DateTo,
            Lines: data.Lines,
            PosTotals: data.PosTotals,
            Signatures: data.Signatures
        );
    }

    private static string GenerateRcaId(string companyCode, string warehouseCode, DateOnly dateTo)
        => $"RCA/{companyCode}/{warehouseCode}/{dateTo:ddMMyyyy}";

    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
            return [warehouseContext.WarehouseId.Value];

        var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
        if (!hasGlobal)
            return (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();

        return null;
    }
}
