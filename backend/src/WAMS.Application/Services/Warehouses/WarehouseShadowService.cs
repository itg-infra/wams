namespace WAMS.Application.Services.Warehouses;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Exceptions;

public class WarehouseShadowService(
    IWarehouseShadowRepository warehouseRepo,
    IUserRepository userRepo,
    IRbacService rbacService,
    IProvinceRepository provinceRepo,
    ITenantContext? tenantContext = null
) : IWarehouseShadowService
{
    public async Task<PaginatedResponse<WarehouseResponse>> GetAllAsync(
        long userId,
        WarehouseQuery query,
        CancellationToken ct = default
    )
    {
        var hasGlobal = await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct);

        (List<WarehouseShadow> Items, int TotalCount) result;

        if (hasGlobal)
        {
            result = await warehouseRepo.GetAllAsync(query, ct);
        }
        else
        {
            var ids = await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct);
            result = await warehouseRepo.GetByIdsAsync(ids, query, ct);
        }

        var totalPages = (int)Math.Ceiling((double)result.TotalCount / query.Limit);

        return new PaginatedResponse<WarehouseResponse>(
            true,
            [.. result.Items.Select(MapToResponse)],
            new PaginationMeta(query.Page, query.Limit, result.TotalCount, totalPages)
        );
    }

    public async IAsyncEnumerable<WarehouseResponse> StreamAllAsync(
        long userId,
        WarehouseQuery query,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var hasGlobal = await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct);

        IAsyncEnumerable<WarehouseResponse> stream;

        if (hasGlobal)
        {
            stream = warehouseRepo.StreamAllAsync(query, limit, ct);
        }
        else
        {
            var ids = await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct);
            stream = warehouseRepo.StreamByIdsAsync(ids, query, limit, ct);
        }

        await foreach (var item in stream.WithCancellation(ct))
        {
            yield return item;
        }
    }

    public async Task<WarehouseResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default)
    {
        var warehouse = await warehouseRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(id));

        await EnsureAccessAsync(userId, id, ct);

        return MapToResponse(warehouse);
    }

    public async Task<List<ProvinceOption>> GetDistinctLocationsAsync(long userId, CancellationToken ct = default)
    {
        var all = await provinceRepo.GetAllActiveAsync(ct);

        if (await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct))
            return [.. all.Select(p => new ProvinceOption(p.Id, p.Name, p.Display))];

        var warehouseIds = await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct);
        var allowed = (await warehouseRepo.GetProvinceIdsForWarehousesAsync(warehouseIds, ct)).ToHashSet();

        return [.. all.Where(p => allowed.Contains(p.Id)).Select(p => new ProvinceOption(p.Id, p.Name, p.Display))];
    }

    public async Task<List<WarehouseResponse>> GetUnmappedAsync(long userId, CancellationToken ct = default)
    {
        if (!await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct))
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);

        var warehouses = await warehouseRepo.GetUnmappedAsync(ct);

        return [.. warehouses.Select(MapToResponse)];
    }

    private async Task EnsureAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        var hasGlobal = await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct);
        if (hasGlobal) return;

        var ids = await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct);
        if (!ids.Contains(warehouseId))
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }

    private static WarehouseResponse MapToResponse(WarehouseShadow w) => new(
        w.Id,
        w.Code,
        w.Name,
        w.Location,
        w.IsActive,
        w.FirstSeenAt,
        w.SyncedAt,
        w.ProvinceId,
        w.Province?.Name,
        w.Province?.Display
    );
}
