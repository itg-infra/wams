namespace WAMS.Application.Services.Spk;

using System.Runtime.CompilerServices;
using WAMS.Application.DTOs.Spk;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Exceptions;

public class SpkService(
    ISpkShadowRepository spkRepo,
    IWarehouseShadowRepository warehouseRepo,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IRbacService rbacService
) : ISpkService
{
    public async Task<(List<SpkShadow> Items, int TotalCount)> GetAllAsync(
        SpkQuery query,
        long userId,
        CancellationToken ct = default
    )
    {
        var whsCodes = await ResolveWhsCodesAsync(userId, ct);

        return await spkRepo.GetAllAsync(query, whsCodes, ct);
    }

    public IAsyncEnumerable<SpkShadowResponse> StreamAllAsync(
        SpkQuery query,
        long userId,
        int limit,
        CancellationToken ct = default
    )
        => StreamAllInternalAsync(query, userId, limit, ct);

    public async Task<SpkShadow> GetByIdAsync(long id, long userId, CancellationToken ct = default)
    {
        var whsCodes = await ResolveWhsCodesAsync(userId, ct);

        return await spkRepo.GetByIdAsync(id, whsCodes, ct)
            ?? throw new NotFoundException(ErrorMessages.Spk.NotFound(id));
    }

    private async IAsyncEnumerable<SpkShadowResponse> StreamAllInternalAsync(
        SpkQuery query,
        long userId,
        int limit,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        var whsCodes = await ResolveWhsCodesAsync(userId, ct);
        await foreach (var item in spkRepo.StreamAllAsync(query, whsCodes, limit, ct))
            yield return item;
    }

    private async Task<IReadOnlyList<string>?> ResolveWhsCodesAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);
            return await warehouseRepo.GetCodesByIdsAsync([warehouseContext.WarehouseId.Value], ct);
        }

        if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
            {
                var warehouseIds = await userRepo.GetUserWarehouseIdsAsync(userId, ct);
                return await warehouseRepo.GetCodesByIdsAsync(warehouseIds, ct);
            }
        }

        return null;
    }

    private async Task EnsureWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        var (exists, hasAccess) = await userRepo.CheckWarehouseAccessAsync(userId, warehouseId, ct);

        if (!exists)
            throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));

        if (!hasAccess)
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }
}
