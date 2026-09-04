namespace WAMS.Application.Services.WorkOrders;

using System.Text.Json;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.TransportOrders;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using WAMS.Domain.ValueObjects;
using DomainValidationException = Domain.Exceptions.ValidationException;

public class WorkOrderService(
    IWorkOrderRepository woRepo,
    IBudgetPlanRepository bpRepo,
    ITransportOrderShadowRepository toRepo,
    IRecapWorkOrderRepository recapRepo,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IRbacService rbacService,
    ICodeCounterRepository codeCounterRepo,
    IUnitOfWork uow,
    IWamsMetrics metrics,
    FluentValidation.IValidator<UpdateWorkOrderRequest> updateValidator,
    IAuditLogWriter auditLogWriter
) : IWorkOrderService
{
    public async Task<(List<WorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        WorkOrderQuery q,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await woRepo.GetAllAsync(q, warehouseIds, ct);
    }

    public async IAsyncEnumerable<WorkOrderSummaryResponse> StreamAllAsync(
        WorkOrderQuery q,
        long userId,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        await foreach (var item in woRepo.StreamAllAsync(q, warehouseIds, limit, ct))
        {
            yield return item;
        }
    }

    public async Task<WorkOrderResponse> GetByIdAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseShadowId = await woRepo.GetWarehouseShadowIdAsync(id, ct);

        if (warehouseShadowId is null)
            throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, warehouseShadowId.Value, ct);

        return await woRepo.GetByIdProjectionAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));
    }

    public async Task<List<WorkOrderPicCandidateResponse>> GetPicCandidatesAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var context = await woRepo.GetPicContextAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, context.WarehouseShadowId, ct);

        var users = await GetEligiblePicUsersAsync(context.CompanyId, context.WarehouseShadowId, ct);

        return [.. users.Select(u => new WorkOrderPicCandidateResponse(u.Id, u.Fullname))];
    }

    public async Task<(List<ApprovedBpForWoResponse> Items, int Total)> GetApprovedBpListAsync(
        long userId,
        int page,
        int limit,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await woRepo.GetApprovedBpListAsync(warehouseIds, page, limit, ct);
    }

    public async Task BulkCreateDraftAsync(
        long budgetPlanId,
        long actorUserId,
        CancellationToken ct = default
    )
    {
        var bp = await bpRepo.GetForWoCreateAsync(budgetPlanId, ct);
        if (bp is null) return;

        var itemsNeedingWo = new List<BpItemForWo>();

        foreach (var item in bp.Items)
        {
            if (!await woRepo.HasActiveWorkOrderForItemAsync(item.Id, ct))
                itemsNeedingWo.Add(item);
        }

        if (itemsNeedingWo.Count == 0) return;

        var prefix = $"WO-{DateTime.UtcNow:yyMM}";
        var startSeq = await codeCounterRepo.NextRangeAsync(prefix, itemsNeedingWo.Count, ct);

        var stubs = itemsNeedingWo.Select((item, index) => new WorkOrder
        {
            Code = $"{prefix}{startSeq + index:D6}",
            BudgetPlanId = bp.Id,
            BudgetPlanItemId = item.Id,
            ItemShadowId = item.ItemShadowId,
            ActivityTypeCode = item.ActivityTypeCode,
            WarehouseShadowId = bp.WarehouseShadowId,
            TemplateCode = bp.TemplateCode,
            IsRfba = item.IsRfba,
            CompanyId = bp.CompanyId,
            Status = WorkOrderStatus.Draft,
            CreatedByUserId = actorUserId,
        }).ToList();

        await woRepo.BulkInsertAsync(stubs, ct);
    }

    public async Task<WorkOrderResponse> UpdateAsync(
        long id,
        UpdateWorkOrderRequest request,
        long userId,
        CancellationToken ct = default
    )
    {
        var validation = updateValidator.Validate(request);

        if (!validation.IsValid) throw new DomainValidationException(validation.Errors.First().ErrorMessage);

        var wo = await woRepo.GetByIdForUpdateAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, wo.WarehouseShadowId, ct);

        if (!wo.Status.CanBeEdited)
            throw new DomainValidationException(ErrorMessages.WorkOrder.CannotUpdateOnlyDraft);

        // check if the budget plan is locked
        await EnsureNotLockedAsync(wo.BudgetPlanId, ct);

        if (request.PicUserId.HasValue && request.PicUserId.Value != wo.PicUserId)
        {
            var eligibleUsers = await GetEligiblePicUsersAsync(wo.CompanyId, wo.WarehouseShadowId, ct);

            if (!eligibleUsers.Any(u => u.Id == request.PicUserId.Value))
                throw new DomainValidationException(ErrorMessages.WorkOrder.PicUserNotFound(request.PicUserId.Value));

            wo.PicUserId = request.PicUserId.Value;
        }
        if (request.StartDate.HasValue) wo.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) wo.EndDate = request.EndDate.Value;
        if (request.CodeBlock is not null) wo.CodeBlock = request.CodeBlock;
        if (request.Notes is not null) wo.Notes = request.Notes;
        if (request.GpsLocation is not null) wo.GpsLocation = MapGps(request.GpsLocation);

        ValidateItemBlNumbers(request.UnloadingItems, request.LoadingItems);

        // Snapshot old child state before mutations
        var oldChildValues = BuildChildSnapshot(wo);

        ReplaceDetails(wo, request);
        await ReplaceTransportOrdersAsync(wo, request.TransportOrderShadowIds, ct);

        wo.UpdatedAt = DateTime.UtcNow;

        // Tracked entity - EF change-tracker emits only the actually-modified UPDATE/INSERT/DELETE.
        await uow.CommitAsync(ct);

        // Only write audit if children were part of the update request AND content actually changed
        var childrenRequested = request.UnloadingItems is not null
            || request.LoadingItems is not null
            || request.TransportOrderShadowIds is not null;

        if (childrenRequested)
        {
            var newChildValues = BuildChildSnapshot(wo);
            if (oldChildValues != newChildValues)
            {
                await auditLogWriter.LogAsync(
                    action: "UPDATE",
                    tableName: "work_orders",
                    recordId: wo.Id,
                    userId: userId,
                    companyId: wo.CompanyId,
                    oldValues: oldChildValues,
                    newValues: newChildValues,
                    ct: ct
                );
            }
        }

        // Read-side projection: one SQL round-trip with JSON-aggregated collections.
        return await woRepo.GetByIdProjectionAsync(wo.Id, ct)
            ?? throw new InvalidOperationException($"Work order {wo.Id} not found after update");
    }

    public async Task DeleteAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var wo = await woRepo.GetByIdForUpdateAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, wo.WarehouseShadowId, ct);

        if (!wo.Status.CanBeDeleted)
            throw new DomainValidationException(ErrorMessages.WorkOrder.CannotDeleteOnlyDraft);

        await EnsureNotLockedAsync(wo.BudgetPlanId, ct);

        await woRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);

        await auditLogWriter.LogAsync(
            action: "DELETE",
            tableName: "work_orders",
            recordId: id,
            userId: userId,
            companyId: wo.CompanyId,
            ct: ct
        );
    }

    public async Task<WorkOrderResponse> SubmitAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var wo = await woRepo.GetByIdForUpdateAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.WorkOrder.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, wo.WarehouseShadowId, ct);

        if (!wo.Status.CanBeSubmitted)
            throw new DomainValidationException(ErrorMessages.WorkOrder.CannotSubmitOnlyDraft);

        await EnsureNotLockedAsync(wo.BudgetPlanId, ct);

        if (wo.StartDate is null || wo.EndDate is null)
            throw new DomainValidationException(ErrorMessages.WorkOrder.DatesRequiredBeforeSubmit);

        if (wo.GpsLocation is null)
            throw new DomainValidationException(ErrorMessages.WorkOrder.GpsRequiredBeforeSubmit);

        if (wo.PicUserId is null)
            throw new DomainValidationException(ErrorMessages.WorkOrder.PicRequiredBeforeSubmit);

        ValidateActivityDetails(wo);

        var submittedAt = DateTime.UtcNow;
        var bpId = wo.BudgetPlanId;
        var companyId = wo.CompanyId;
        await uow.ExecuteInTransactionAsync(async ct =>
        {
            await woRepo.SubmitAsync(wo.Id, userId, submittedAt, ct);
            await recapRepo.UpsertForBudgetPlanAsync(bpId, companyId, ct);
        }, ct);

        await auditLogWriter.LogAsync(
            action: "UPDATE",
            tableName: "work_orders",
            recordId: wo.Id,
            userId: userId,
            companyId: wo.CompanyId,
            newValues: JsonSerializer.Serialize(new { Status = "Submitted", SubmittedByUserId = userId }),
            ct: ct
        );

        metrics.RecordWorkOrderSubmitted(companyId);

        return await woRepo.GetByIdProjectionAsync(wo.Id, ct)
            ?? throw new InvalidOperationException($"Work order {wo.Id} not found after submit");
    }

    // Set-diff replacement for the W↔TO link table on a tracked WorkOrder.
    // - Removes TO links no longer in the desired set (tracked deletes → batched DELETE).
    // - Adds links not yet present (tracked inserts → batched INSERT).
    // Avoids the old "delete all, re-insert all" anti-pattern from the AsNoTracking repo.
    private async Task ReplaceTransportOrdersAsync(
        WorkOrder wo,
        List<long>? shadowIds,
        CancellationToken ct
    )
    {
        if (shadowIds is null) return;

        var desired = shadowIds.Distinct().ToHashSet();
        var current = wo.TransportOrders.ToDictionary(t => t.TransportOrderShadowId);

        foreach (var (k, t) in current.Where(kv => !desired.Contains(kv.Key)).ToList())
            wo.TransportOrders.Remove(t);

        var toAdd = desired.Where(id => !current.ContainsKey(id)).ToList();
        if (toAdd.Count == 0) return;

        var shadows = await toRepo.GetByIdsAsync(toAdd, ct);
        var shadowMap = shadows.ToDictionary(s => s.Id);
        foreach (var sid in toAdd)
        {
            if (!shadowMap.TryGetValue(sid, out var s))
                throw new NotFoundException(ErrorMessages.TransportOrder.ShadowNotFound(sid));

            wo.TransportOrders.Add(new WorkOrderTransportOrder
            {
                TransportOrderShadowId = sid,
                TransportOrderShadow = s,
            });
        }
    }

    private static GpsCoordinate? MapGps(GpsLocationRequest? r) =>
        r is null ? null : new GpsCoordinate(r.Latitude, r.Longitude, r.Accuracy, r.RecordedAt);

    private static void ValidateActivityDetails(WorkOrder wo)
    {
        switch (wo.ActivityTypeCode)
        {
            case ActivityTypeCodes.Bongkar:
                if (wo.UnloadingItems.Count == 0)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresUnloadingItem);
                break;
            case ActivityTypeCodes.Muat:
                if (wo.LoadingItems.Count == 0)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresLoadingItem);
                break;
            case ActivityTypeCodes.Fumigasi:
                if (wo.FumigationDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresFumigationDetail);
                break;
            case ActivityTypeCodes.Qc:
                if (wo.QcDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresQcDetail);
                break;
            case ActivityTypeCodes.Gudang:
            case ActivityTypeCodes.Opname:
            case ActivityTypeCodes.Others:
                if (wo.StorageDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresStorageHandlingDetail(wo.ActivityTypeCode));
                break;
            case ActivityTypeCodes.AlatBerat:
                if (wo.HeavyEquipDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresHeavyEquipmentDetail);
                break;
            case ActivityTypeCodes.Unbagging:
                if (wo.UnbaggingDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresUnbaggingDetail);
                break;
            case ActivityTypeCodes.Rebagging:
                if (wo.RebaggingDetail is null)
                    throw new DomainValidationException(ErrorMessages.WorkOrder.RequiresRebaggingDetail);
                break;
        }
    }

    // Lock guard
    private async Task EnsureNotLockedAsync(long budgetPlanId, CancellationToken ct)
    {
        if (await recapRepo.IsApprovedByBudgetPlanIdAsync(budgetPlanId, ct))
            throw new ConflictException(ErrorMessages.WorkOrder.LockedRecapApproved);
    }

    private async Task EnsureWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        var (exists, hasAccess) = await userRepo.CheckWarehouseAccessAsync(userId, warehouseId, ct);

        if (!exists)
            throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));

        if (!hasAccess)
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }

    // PIC eligibility is granted via workorder.workorder.execute, not tied to a specific role.
    private Task<List<User>> GetEligiblePicUsersAsync(long companyId, long warehouseId, CancellationToken ct) =>
        userRepo.GetUsersByPermissionAndWarehouseAsync(companyId, warehouseId, Permissions.WorkOrder.Execute, ct);

    // Warehouse scope resolution
    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
            return [warehouseContext.WarehouseId.Value];

        if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
                return (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();
        }

        return null; // SuperAdmin bypass - no filter
    }

    // Helpers
    private static void ValidateItemBlNumbers(
        List<CreateUnloadingItemRequest>? unloading,
        List<CreateLoadingItemRequest>? loading
    )
    {
        if (unloading is not null && unloading.Any(i => string.IsNullOrWhiteSpace(i.BlNumber)))
            throw new DomainValidationException(ErrorMessages.WorkOrder.UnloadingBlNumberRequired);
        if (loading is not null && loading.Any(i => string.IsNullOrWhiteSpace(i.BlNumber)))
            throw new DomainValidationException(ErrorMessages.WorkOrder.LoadingBlNumberRequired);
    }

    private static void ReplaceDetails(WorkOrder wo, UpdateWorkOrderRequest req)
    {
        if (req.UnloadingItems is not null)
        {
            wo.UnloadingItems.Clear();
            foreach (var item in req.UnloadingItems)
                wo.UnloadingItems.Add(MapUnloadingItem(item));
        }

        if (req.LoadingItems is not null)
        {
            wo.LoadingItems.Clear();
            foreach (var item in req.LoadingItems)
                wo.LoadingItems.Add(MapLoadingItem(item));
        }

        if (req.Fumigation is not null)
        {
            if (wo.FumigationDetail is not null)
                ApplyFumigationDetail(wo.FumigationDetail, req.Fumigation);
            else
                wo.FumigationDetail = MapFumigationDetail(req.Fumigation);
        }

        var storage = req.Storage ?? req.Others;
        if (storage is not null)
        {
            if (wo.StorageDetail is not null)
                ApplyStorageDetail(wo.StorageDetail, storage);
            else
                wo.StorageDetail = MapStorageDetail(storage);
        }

        if (req.Qc is not null)
        {
            if (wo.QcDetail is not null)
                ApplyQcDetail(wo.QcDetail, req.Qc);
            else
                wo.QcDetail = MapQcDetail(req.Qc);
        }

        if (req.HeavyEquipment is not null)
        {
            if (wo.HeavyEquipDetail is not null)
                ApplyHeavyEquipDetail(wo.HeavyEquipDetail, req.HeavyEquipment);
            else
                wo.HeavyEquipDetail = MapHeavyEquipDetail(req.HeavyEquipment);
        }

        if (req.Unbagging is not null)
        {
            if (wo.UnbaggingDetail is not null)
                ApplyUnbaggingDetail(wo.UnbaggingDetail, req.Unbagging);
            else
                wo.UnbaggingDetail = MapUnbaggingDetail(req.Unbagging);
        }

        if (req.Rebagging is not null)
        {
            if (wo.RebaggingDetail is not null)
                ApplyRebaggingDetail(wo.RebaggingDetail, req.Rebagging);
            else
                wo.RebaggingDetail = MapRebaggingDetail(req.Rebagging);
        }
    }

    private static WorkOrderUnloadingItem MapUnloadingItem(CreateUnloadingItemRequest r) => new()
    {
        SpkShadowId = r.SpkShadowId,
        BlNumber = r.BlNumber,
        ProductName = r.ProductName,
        Quantity = r.Quantity,
        UomCode = r.UomCode,
        NoVehicle = r.NoVehicle,
        NoContainer = r.NoContainer,
        NoSeal = r.NoSeal,
        GrossWeight = r.GrossWeight,
        FinalWeight = r.FinalWeight,
        NettWeight = r.NettWeight,
        TotalBag = r.TotalBag,
        UnitWeight = r.UnitWeight,
        IsChecked = r.IsChecked,
        SortOrder = r.SortOrder,
    };

    private static WorkOrderLoadingItem MapLoadingItem(CreateLoadingItemRequest r) => new()
    {
        SpkShadowId = r.SpkShadowId,
        BlNumber = r.BlNumber,
        ProductName = r.ProductName,
        Quantity = r.Quantity,
        UomCode = r.UomCode,
        NoVehicle = r.NoVehicle,
        NoContainer = r.NoContainer,
        NoSeal = r.NoSeal,
        GrossWeight = r.GrossWeight,
        FinalWeight = r.FinalWeight,
        NettWeight = r.NettWeight,
        TotalBag = r.TotalBag,
        UnitWeight = r.UnitWeight,
        IsChecked = r.IsChecked,
        SortOrder = r.SortOrder,
    };

    private static void ApplyFumigationDetail(WorkOrderFumigationDetail d, CreateFumigationDetailRequest r)
    {
        d.FumiId = r.FumiId;
        d.TotalDuration = r.TotalDuration;
        d.BlNumber = r.BlNumber;
        d.MvName = r.MvName;
        d.InitialTemperature = r.InitialTemperature;
        d.FinalTemperature = r.FinalTemperature;
        d.FumigationType = r.FumigationType;
        d.MethylBromideDosage = r.MethylBromideDosage;
        d.SulphurFluorideDosage = r.SulphurFluorideDosage;
        d.PhosphineDosage = r.PhosphineDosage;
        d.Result = r.Result;
    }

    private static void ApplyStorageDetail(WorkOrderStorageDetail d, CreateStorageDetailRequest r)
    {
        d.HasPindahStapel = r.HasPindahStapel;
        d.HasPembersihan = r.HasPembersihan;
        d.HasPerapihan = r.HasPerapihan;
        d.VolumeWeight = r.VolumeWeight;
        d.WorkerOnDuty = r.WorkerOnDuty;
        d.HasMask = r.HasMask;
        d.HasSafetyGlasses = r.HasSafetyGlasses;
        d.HasHandGloves = r.HasHandGloves;
        d.HasHelmet = r.HasHelmet;
        d.HasSafetyShoes = r.HasSafetyShoes;
        d.HasSafetyVest = r.HasSafetyVest;
    }

    private static void ApplyQcDetail(WorkOrderQcDetail d, CreateQcDetailRequest r)
    {
        d.MoisturePercent = r.MoisturePercent;
        d.JamurPercent = r.JamurPercent;
        d.BauPercent = r.BauPercent;
        d.QualityStatus = r.QualityStatus;
    }

    private static void ApplyHeavyEquipDetail(WorkOrderHeavyEquipDetail d, CreateHeavyEquipDetailRequest r)
    {
        d.BlNumber = r.BlNumber;
        d.StartTime = r.StartTime;
        d.EndTime = r.EndTime;
        d.StandbyDuration1 = r.StandbyDuration1;
        d.StandbyDuration2 = r.StandbyDuration2;
        d.MinimumDuration = r.MinimumDuration;
        d.CostPerHour = r.CostPerHour;
        d.TotalCost = r.TotalCost;
    }

    private static void ApplyUnbaggingDetail(WorkOrderUnbaggingDetail d, CreateUnbaggingDetailRequest r)
    {
        d.NoVehicle = r.NoVehicle;
        d.NoContainer = r.NoContainer;
        d.NoSeal = r.NoSeal;
        d.InitialWeight = r.InitialWeight;
        d.FinalWeight = r.FinalWeight;
        d.UnitWeight = r.UnitWeight;
        d.TotalWeight = r.TotalWeight;
        d.TotalBag = r.TotalBag;
    }

    private static void ApplyRebaggingDetail(WorkOrderRebaggingDetail d, CreateRebaggingDetailRequest r)
    {
        d.Receiver = r.Receiver;
        d.NoVehicle = r.NoVehicle;
        d.NoContainer = r.NoContainer;
        d.NoSeal = r.NoSeal;
        d.InitialWeight = r.InitialWeight;
        d.FinalWeight = r.FinalWeight;
        d.TotalWeight = r.TotalWeight;
    }

    private static WorkOrderFumigationDetail MapFumigationDetail(CreateFumigationDetailRequest r) => new()
    {
        FumiId = r.FumiId,
        TotalDuration = r.TotalDuration,
        BlNumber = r.BlNumber,
        MvName = r.MvName,
        InitialTemperature = r.InitialTemperature,
        FinalTemperature = r.FinalTemperature,
        FumigationType = r.FumigationType,
        MethylBromideDosage = r.MethylBromideDosage,
        SulphurFluorideDosage = r.SulphurFluorideDosage,
        PhosphineDosage = r.PhosphineDosage,
        Result = r.Result,
    };

    private static WorkOrderStorageDetail MapStorageDetail(CreateStorageDetailRequest r) => new()
    {
        HasPindahStapel = r.HasPindahStapel,
        HasPembersihan = r.HasPembersihan,
        HasPerapihan = r.HasPerapihan,
        VolumeWeight = r.VolumeWeight,
        WorkerOnDuty = r.WorkerOnDuty,
        HasMask = r.HasMask,
        HasSafetyGlasses = r.HasSafetyGlasses,
        HasHandGloves = r.HasHandGloves,
        HasHelmet = r.HasHelmet,
        HasSafetyShoes = r.HasSafetyShoes,
        HasSafetyVest = r.HasSafetyVest,
    };

    private static WorkOrderQcDetail MapQcDetail(CreateQcDetailRequest r) => new()
    {
        MoisturePercent = r.MoisturePercent,
        JamurPercent = r.JamurPercent,
        BauPercent = r.BauPercent,
        QualityStatus = r.QualityStatus,
    };

    private static WorkOrderHeavyEquipDetail MapHeavyEquipDetail(CreateHeavyEquipDetailRequest r) => new()
    {
        BlNumber = r.BlNumber,
        StartTime = r.StartTime,
        EndTime = r.EndTime,
        StandbyDuration1 = r.StandbyDuration1,
        StandbyDuration2 = r.StandbyDuration2,
        MinimumDuration = r.MinimumDuration,
        CostPerHour = r.CostPerHour,
        TotalCost = r.TotalCost,
    };

    private static WorkOrderUnbaggingDetail MapUnbaggingDetail(CreateUnbaggingDetailRequest r) => new()
    {
        NoVehicle = r.NoVehicle,
        NoContainer = r.NoContainer,
        NoSeal = r.NoSeal,
        InitialWeight = r.InitialWeight,
        FinalWeight = r.FinalWeight,
        UnitWeight = r.UnitWeight,
        TotalWeight = r.TotalWeight,
        TotalBag = r.TotalBag,
    };

    private static WorkOrderRebaggingDetail MapRebaggingDetail(CreateRebaggingDetailRequest r) => new()
    {
        Receiver = r.Receiver,
        NoVehicle = r.NoVehicle,
        NoContainer = r.NoContainer,
        NoSeal = r.NoSeal,
        InitialWeight = r.InitialWeight,
        FinalWeight = r.FinalWeight,
        TotalWeight = r.TotalWeight,
    };

    private static string BuildChildSnapshot(WorkOrder wo)
    {
        var snapshot = new Dictionary<string, object?>
        {
            ["unloading_items"] = wo.UnloadingItems.Select(i => new
            {
                i.Id,
                i.BlNumber,
                i.ProductName,
                i.Quantity,
                i.UomCode,
                i.NoVehicle,
                i.GrossWeight,
                i.NettWeight,
                i.TotalBag,
                i.SortOrder
            }).ToList(),
            ["loading_items"] = wo.LoadingItems.Select(i => new
            {
                i.Id,
                i.BlNumber,
                i.ProductName,
                i.Quantity,
                i.UomCode,
                i.NoVehicle,
                i.GrossWeight,
                i.NettWeight,
                i.TotalBag,
                i.SortOrder
            }).ToList(),
            ["transport_order_shadow_ids"] = wo.TransportOrders
                .Select(t => t.TransportOrderShadowId).ToList()
        };

        return JsonSerializer.Serialize(snapshot);
    }
}
