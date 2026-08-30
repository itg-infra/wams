namespace WAMS.Application.Services.BudgetPlans;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WAMS.Application.Common;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class BudgetPlanService(
    IBudgetPlanRepository budgetPlanRepo,
    IBudgetTemplateRepository budgetTemplateRepo,
    IWarehouseShadowRepository warehouseRepo,
    IRateCardRepository rateCardRepo,
    IVendorShadowRepository vendorRepo,
    ISpkShadowRepository spkRepo,
    IItemShadowRepository itemShadowRepo,
    ICodeCounterRepository codeCounterRepo,
    IUomMasterRepository uomRepo,
    IActivityTypeRepository activityTypeRepo,
    IUnitOfWork uow,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IWorkflowRepository workflowRepo,
    INotificationService notificationService,
    IRbacService rbacService,
    IWamsMetrics metrics,
    IWorkOrderService woService,
    IRecapWorkOrderRepository recapRepo,
    ITenantContext tenantContext,
    IAuditLogWriter auditLogWriter,
    ILogger<BudgetPlanService> logger
) : IBudgetPlanService
{
    private const string DocType = WorkflowDocTypes.BudgetPlanApproval;

    public async Task<(List<BudgetPlanSummaryResponse> Items, int TotalCount)> GetAllAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery query,
        long userId,
        CancellationToken ct = default
    )
    {
        IReadOnlyList<long>? warehouseIds = null;

        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);
            warehouseIds = [warehouseContext.WarehouseId.Value];
        }
        else if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal)
                warehouseIds = (await userRepo.GetUserWarehouseIdsAsync(userId, ct)).ToList();
        }

        var (data, total) = await budgetPlanRepo.GetAllSummaryAsync(status, query, warehouseIds, ct);

        return (data, total);
    }

    public async IAsyncEnumerable<BudgetPlanSummaryResponse> StreamAllAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery query,
        long userId,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        IReadOnlyList<long>? warehouseIds = null;

        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);
            warehouseIds = [warehouseContext.WarehouseId.Value];
        }
        else if (!warehouseContext.IsSet)
        {
            var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
            if (!hasGlobal) warehouseIds = [.. await userRepo.GetUserWarehouseIdsAsync(userId, ct)];
        }

        await foreach (
            var item in budgetPlanRepo.StreamAllAsync(
                status, 
                query, 
                warehouseIds, 
                limit, 
                ct
            )
        )
            yield return item;
    }

    public async Task<BudgetPlanResponse> GetByIdAsync(
        long id,
        long userId,
        CancellationToken ct = default,
        long? vendorShadowId = null
    )
    {
        var warehouseShadowId = await budgetPlanRepo.GetWarehouseShadowIdAsync(id, ct);

        if (warehouseShadowId is null)
            throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, warehouseShadowId.Value, ct);

        var result = await budgetPlanRepo.GetByIdProjectionAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        if (!vendorShadowId.HasValue)
            return result;

        var items = result.Items
            .Where(item => item.VendorShadowId == vendorShadowId.Value)
            .ToList();

        return result with
        {
            Items = items,
            GrandTotal = items.Sum(item => item.TotalValue),
            TotalPpnAmount = items.Sum(item => item.PpnAmount),
            TotalPphAmount = items.Sum(item => item.PphAmount),
            TaxInclusiveGrandTotal = items.Sum(item => item.GrandTotal),
        };
    }

    public async Task<BudgetPlanResponse> CreateAsync(
        long userId,
        CreateBudgetPlanRequest request,
        CancellationToken ct = default
    )
    {
        var plan = await CreateCoreAsync(userId, request, ct);

        return await budgetPlanRepo.GetByIdProjectionAsync(plan.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFoundAfterCreation);
    }

    public async Task<BudgetPlanResponse> CreateAndSubmitAsync(
        long userId,
        CreateBudgetPlanRequest request,
        CancellationToken ct = default
    )
    {
        var plan = await CreateCoreAsync(userId, request, ct);

        await InitiateWorkflowAsync(plan, userId, ct);

        return await budgetPlanRepo.GetByIdProjectionAsync(plan.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFoundAfterSubmit(plan.Id));
    }

    private async Task<BudgetPlan> CreateCoreAsync(
        long userId,
        CreateBudgetPlanRequest request,
        CancellationToken ct
    )
    {
        var template = await budgetTemplateRepo.GetByIdForPlanSourceAsync(request.BudgetTemplateId, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFound(request.BudgetTemplateId));

        if (template.Status != BudgetTemplateStatus.Submitted)
            throw new ValidationException(ErrorMessages.BudgetTemplate.OnlySubmittedCanBeUsed);

        var warehouse = await warehouseRepo.GetByIdAsync(request.WarehouseShadowId, ct)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(request.WarehouseShadowId));

        EnsureWarehouseBelongsToTenant(warehouse);

        if (warehouse.ProvinceId != template.ProvinceId)
            throw new ValidationException(
                ErrorMessages.BudgetPlan.WarehouseProvinceMismatch);

        await EnsureWarehouseAccessAsync(userId, request.WarehouseShadowId, ct);

        var prefix = $"BP-{DateTime.UtcNow:yyMM}";
        var code = await DocumentCodeGenerator.NextCodeAsync(codeCounterRepo, prefix, ct);

        var plan = new BudgetPlan
        {
            Code = code,
            BudgetTemplateId = request.BudgetTemplateId,
            WarehouseShadowId = request.WarehouseShadowId,
            Remark = request.Remark,
            DocDate = request.DocDate,
            Status = BudgetPlanStatus.Draft,
            CreatedByUserId = userId,
        };

        // Load SPK map first: one query shared for both adding SPK items and validating cost item links.
        var spkItemCodeMap = await AddSpkItemsAsync(plan, request.SpkShadowIds, userId, ct);
        await AddItemsAsync(plan, request.Items, spkItemCodeMap, ct);

        await budgetPlanRepo.CreateAsync(plan, ct);
        await uow.CommitAsync(ct);

        return plan;
    }

    public async Task<BudgetPlanResponse> UpdateAsync(
        long id,
        UpdateBudgetPlanRequest request,
        long userId,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        if (!plan.Status.CanBeEdited)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotUpdateOnlyDraftOrRejected);

        if (request.WarehouseShadowId.HasValue)
        {
            var warehouse = await warehouseRepo.GetByIdAsync(request.WarehouseShadowId.Value, ct)
                ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(request.WarehouseShadowId.Value));

            EnsureWarehouseBelongsToTenant(warehouse);

            if (warehouse.ProvinceId != plan.BudgetTemplate.ProvinceId)
                throw new ValidationException(
                    ErrorMessages.BudgetPlan.WarehouseProvinceMismatch);

            plan.WarehouseShadowId = request.WarehouseShadowId.Value;
        }

        if (request.Remark is not null)
            plan.Remark = request.Remark;

        if (request.DocDate.HasValue)
            plan.DocDate = request.DocDate.Value;

        // Resolve the SPK map first so AddItemsAsync can validate SpkShadowId references
        // without a second DB round-trip. If SPK items are being replaced, load the new set;
        // otherwise derive the map from the already-loaded plan.SpkItems navigation.
        IReadOnlyDictionary<long, (string ItemCode, decimal? Quantity)> spkItemCodeMap;
        if (request.SpkShadowIds is not null)
        {
            // Guard: if Items are not being replaced, existing cost items may still reference
            // SPKs that are about to be removed. Reject early rather than persisting orphaned FKs.
            if (request.Items is null)
            {
                var incomingSpkSet = new HashSet<long>(request.SpkShadowIds);
                var orphaned = plan.Items
                    .Where(i => i.SpkShadowId.HasValue && !incomingSpkSet.Contains(i.SpkShadowId.Value))
                    .Select(i => i.SpkShadowId!.Value)
                    .Distinct()
                    .ToList();
                if (orphaned.Count > 0)
                    throw new ValidationException(
                        ErrorMessages.Spk.CannotReplaceSpkListOrphanedItems(string.Join(", ", orphaned)));
            }

            plan.SpkItems.Clear();
            spkItemCodeMap = await AddSpkItemsAsync(plan, request.SpkShadowIds, userId, ct);
        }
        else
        {
            spkItemCodeMap = plan.SpkItems.ToDictionary(s => s.SpkShadowId, s => (s.Spk.ItemCode, s.Spk.Quantity));
        }

        // Items are matched to their prior state by ItemShadowId (the same identity key already used
        // for realization grouping in RecapWorkOrderService) - CreateBudgetPlanItemRequest has no Id field,
        // so ItemShadowId is the only stable cross-reference available between old and new item lists.
        string? oldItemsSnapshot = null;

        if (request.Items is not null)
        {
            // ANY WorkOrder (active or soft-deleted) still holds a live Restrict FK to
            // its BudgetPlanItem, so these item shadows can never be removed from the request.
            var itemShadowIdsWithAnyWorkOrder = plan.Items
                .Where(i => plan.WorkOrders.Any(w => w.BudgetPlanItemId == i.Id))
                .Select(i => i.ItemShadowId)
                .Distinct()
                .ToList();

            // Business-rule set: only active WorkOrders count as "committed spend" for the
            // cannot-reduce-below-committed check - a soft-deleted WorkOrder no longer represents
            // real spend, even though it still mechanically blocks deletion above.
            var itemShadowIdsWithActiveWorkOrders = plan.Items
                .Where(i => plan.WorkOrders.Any(w => w.BudgetPlanItemId == i.Id && w.DeletedAt == null))
                .Select(i => i.ItemShadowId)
                .ToHashSet();

            if (itemShadowIdsWithAnyWorkOrder.Count > 0)
            {
                // An omitted CostValue keeps the existing rate (see the in-place mutation below),
                // so the validation total must resolve it the same way instead of treating it as 0 -
                // otherwise a quantity-only increase on an item with no CostValue override would be
                // rejected as a false reduction below committed spend.
                var existingCostByShadowId = plan.Items.ToDictionary(i => i.ItemShadowId, i => i.CostValue);
                var incomingTotalsByItemShadowId = request.Items
                    .GroupBy(i => i.ItemShadowId)
                    .ToDictionary(g => g.Key, g => g.Sum(i =>
                        (i.CostValue ?? (existingCostByShadowId.TryGetValue(i.ItemShadowId, out var existingCost) ? existingCost : 0m)) * i.Quantity));

                var existingTotalsByItemShadowId = plan.Items
                    .GroupBy(i => i.ItemShadowId)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalValue));

                foreach (var itemShadowId in itemShadowIdsWithAnyWorkOrder)
                {
                    if (!incomingTotalsByItemShadowId.TryGetValue(itemShadowId, out var incomingTotal))
                        throw new ValidationException(ErrorMessages.BudgetPlan.CannotRemoveItemWithWorkOrders(itemShadowId));

                    // In-place mutation only applies one incoming row per WorkOrder-linked item -
                    // multiple rows would sum-pass the committed-total check below while only the
                    // first is actually applied, silently under-committing the persisted total.
                    if (request.Items.Count(i => i.ItemShadowId == itemShadowId) > 1)
                        throw new ValidationException(ErrorMessages.BudgetPlan.CannotSplitItemWithWorkOrders(itemShadowId));

                    if (!itemShadowIdsWithActiveWorkOrders.Contains(itemShadowId))
                        continue;

                    var committedTotal = existingTotalsByItemShadowId[itemShadowId];
                    if (incomingTotal < committedTotal)
                        throw new ValidationException(ErrorMessages.BudgetPlan.CannotReduceItemBelowCommitted(itemShadowId, committedTotal));
                }
            }

            oldItemsSnapshot = JsonSerializer.Serialize(plan.Items.Select(i => new
            {
                i.Id,
                i.ItemShadowId,
                i.CostValue,
                i.Quantity,
                i.TotalValue,
            }));
        }

        if (request.Items is not null)
        {
            // BudgetPlanItem -> WorkOrder is Restrict on delete, so an item with ANY WorkOrder
            // (active or soft-deleted) must never leave the tracked plan.Items collection - the
            // validation above already guarantees the incoming request still accounts for it.
            // Update these in place instead of delete+recreate; only items with no WorkOrder at
            // all are safe to drop-and-replace via AddItemsAsync.
            var workOrderLinkedItems = plan.Items
                .Where(i => plan.WorkOrders.Any(w => w.BudgetPlanItemId == i.Id))
                .ToList();
            var workOrderLinkedShadowIds = workOrderLinkedItems.Select(i => i.ItemShadowId).ToHashSet();

            var firstIncomingByShadowId = new Dictionary<long, CreateBudgetPlanItemRequest>();
            foreach (var r in request.Items)
                firstIncomingByShadowId.TryAdd(r.ItemShadowId, r);

            foreach (var existingItem in workOrderLinkedItems)
            {
                // Validation above guarantees exactly one incoming row per WorkOrder-linked ItemShadowId.
                var match = firstIncomingByShadowId[existingItem.ItemShadowId];

                if (match.CostValue.HasValue && match.CostValue.Value <= 0)
                    throw new ValidationException(ErrorMessages.BudgetPlan.UnitCostOverrideMustBePositive(existingItem.ItemShadowId));

                existingItem.CostValue = match.CostValue ?? existingItem.CostValue;
                existingItem.Quantity = match.Quantity;
                existingItem.TotalValue = existingItem.CostValue * existingItem.Quantity;

                var tax = TaxCalculator.Calculate(existingItem.TotalValue, existingItem.PpnRate, existingItem.PphRate);
                existingItem.PpnAmount = tax.PpnAmount;
                existingItem.PphAmount = tax.PphAmount;
                existingItem.GrandTotal = tax.GrandTotal;
            }

            var removableItems = plan.Items.Where(i => !workOrderLinkedShadowIds.Contains(i.ItemShadowId)).ToList();
            foreach (var item in removableItems)
                plan.Items.Remove(item);

            var genuinelyNewItems = request.Items
                .Where(r => !workOrderLinkedShadowIds.Contains(r.ItemShadowId))
                .ToList();
            if (genuinelyNewItems.Count > 0)
                await AddItemsAsync(plan, genuinelyNewItems, spkItemCodeMap, ct);
        }

        plan.UpdatedAt = DateTime.UtcNow;

        await budgetPlanRepo.UpdateAsync(plan, ct);
        await uow.CommitAsync(ct);

        if (oldItemsSnapshot is not null)
        {
            var newItemsSnapshot = JsonSerializer.Serialize(plan.Items.Select(i => new
            {
                i.Id,
                i.ItemShadowId,
                i.CostValue,
                i.Quantity,
                i.TotalValue,
            }));

            await auditLogWriter.LogAsync(
                action: "UPDATE",
                tableName: "budget_plans",
                recordId: plan.Id,
                companyId: plan.CompanyId,
                oldValues: oldItemsSnapshot,
                newValues: newItemsSnapshot,
                ct: ct
            );
        }

        return await budgetPlanRepo.GetByIdProjectionAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFoundAfterUpdate(id));
    }

    public async Task SubmitAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdForSubmitAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, plan.WarehouseShadowId, ct);

        if (!plan.Status.CanBeSubmitted)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotSubmitOnlyDraftOrRejected);

        if (plan.Items.Count == 0)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotSubmitWithNoItems);

        await InitiateWorkflowAsync(plan, userId, ct);

        metrics.RecordBudgetPlanSubmitted(plan.CompanyId);
    }

    public async Task ApproveAsync(
        long id,
        long userId,
        IReadOnlyList<string> userRoles,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdForApprovalAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, plan.WarehouseShadowId, ct);

        if (!plan.Status.CanBeApproved)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotApprove(plan.Status.DisplayName));

        if (plan.WorkflowInstance is null)
            throw new ValidationException(ErrorMessages.BudgetPlan.NoWorkflow);

        var instance = plan.WorkflowInstance;

        var currentStage = instance.Stages
            .OrderBy(s => s.StageOrder)
            .FirstOrDefault(s => s.Status == WorkflowStageStatus.Pending)
            ?? throw new ValidationException(ErrorMessages.BudgetPlan.NoPendingApprovalStage);

        var hasGlobalAccess = await rbacService.HasGlobalAccessAsync(userId, ct);
        var isAuthorized = hasGlobalAccess || currentStage.ApproverRoles
            .Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase));

        if (!isAuthorized)
            throw new ValidationException(ErrorMessages.BudgetPlan.NotAuthorizedAtStage(currentStage.StageOrder));

        // approval.self.approve lives in its own module so the budget.*.* wildcard (HO_SPV,
        // LOG_MGR, LOG_SPV) doesn't grant it - only *.*.* does, i.e. SUPER_ADMIN.
        if (userId == plan.SubmittedByUserId)
        {
            var key = Permissions.Approval.SelfApprove.Split('.');
            var canSelfApprove = await rbacService.HasPermissionAsync(userId, key[0], key[1], key[2], ct);

            if (!canSelfApprove)
                throw new ValidationException(ErrorMessages.BudgetPlan.CannotApproveOwnSubmission);

            logger.LogWarning(
                "[BPApprove] User {UserId} self-approved plan {PlanId} at stage {Stage} via {Permission}",
                userId, plan.Id, currentStage.StageOrder, Permissions.Approval.SelfApprove);
        }

        currentStage.Status = WorkflowStageStatus.Approved;
        currentStage.ApprovedByUserId = userId;
        currentStage.ApprovedAt = DateTime.UtcNow;
        currentStage.UpdatedAt = DateTime.UtcNow;

        var maxStage = instance.Stages.Max(s => s.StageOrder);
        var isLastStage = currentStage.StageOrder == maxStage;

        if (isLastStage)
        {
            plan.Status = BudgetPlanStatus.Approved;
            await woService.BulkCreateDraftAsync(plan.Id, userId, ct);
        }
        else
        {
            instance.CurrentStageOrder = currentStage.StageOrder + 1;
            instance.UpdatedAt = DateTime.UtcNow;
            plan.Status = BudgetPlanStatus.InApproval;
        }

        plan.UpdatedAt = DateTime.UtcNow;

        await uow.CommitAsync(ct);

        // Recap row created here (not at first WO submit) so it's visible - as Pending, 0
        // realization - the moment Draft WOs exist, instead of only after someone submits one.
        // Placed after CommitAsync: WOs must already be durably persisted before this raw-SQL
        // upsert runs, so a later failure here can't leave a recap row with no WOs behind it.
        if (isLastStage)
            await recapRepo.UpsertForBudgetPlanAsync(plan.Id, plan.CompanyId, ct);

        metrics.RecordBudgetPlanApproved(plan.CompanyId, currentStage.StageOrder);

        await TryPublishNotificationsAsync(
            await BuildApprovalNotificationsAsync(plan, instance, currentStage, userId, ct), ct);
    }

    public async Task RejectAsync(
        long id,
        long userId,
        RejectBudgetPlanRequest request,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdForApprovalAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        await EnsureWarehouseAccessAsync(userId, plan.WarehouseShadowId, ct);

        if (!plan.Status.CanBeRejected)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotRejectOnlySubmittedOrInApproval);

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException(ErrorMessages.BudgetPlan.RejectionReasonRequired);

        if (plan.WorkflowInstance is null)
            throw new ValidationException(ErrorMessages.BudgetPlan.NoWorkflow);

        var instance = plan.WorkflowInstance;

        var currentStage = instance.Stages
            .OrderBy(s => s.StageOrder)
            .FirstOrDefault(s => s.Status == WorkflowStageStatus.Pending)
            ?? throw new ValidationException(ErrorMessages.BudgetPlan.NoPendingApprovalStage);

        currentStage.Status = WorkflowStageStatus.Rejected;
        currentStage.RejectedByUserId = userId;
        currentStage.RejectedAt = DateTime.UtcNow;
        currentStage.RejectionReason = request.Reason;
        currentStage.UpdatedAt = DateTime.UtcNow;

        plan.Status = BudgetPlanStatus.Rejected;
        plan.RejectedByUserId = userId;
        plan.RejectedAt = DateTime.UtcNow;
        plan.RejectionReason = request.Reason;
        plan.UpdatedAt = DateTime.UtcNow;

        await uow.CommitAsync(ct);

        metrics.RecordBudgetPlanRejected(plan.CompanyId);

        await TryPublishNotificationsAsync(BuildRejectNotifications(plan, userId), ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var plan = await budgetPlanRepo.GetSummaryAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(id));

        if (!plan.Status.CanBeDeleted)
            throw new ValidationException(ErrorMessages.BudgetPlan.CannotDeleteOnlyDraft);

        await budgetPlanRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);
    }

    // Creates a fresh WorkflowInstance when submitting (or re-submitting after rejection).
    // Uses EF navigation property assignment so instance insert + plan FK update happen in a single commit.
    // Old instance (if resubmitting) is deleted first to prevent orphan accumulation.
    private async Task InitiateWorkflowAsync(BudgetPlan plan, long userId, CancellationToken ct)
    {
        var workflowTemplate = await workflowRepo.GetActiveTemplateAsync(plan.CompanyId, DocType, ct)
            ?? throw new ValidationException(ErrorMessages.WorkflowTemplate.NoActiveTemplate);

        var stages = workflowTemplate.Stages.OrderBy(s => s.StageOrder).ToList();
        if (stages.Count == 0)
            throw new ValidationException(ErrorMessages.WorkflowTemplate.NoStagesConfigured);

        if (plan.WorkflowInstanceId.HasValue)
        {
            var previousInstance = await workflowRepo.GetInstanceWithStagesAsync(plan.WorkflowInstanceId.Value, ct);
            if (previousInstance is not null)
            {
                var snapshot = JsonSerializer.Serialize(new
                {
                    previousInstance.Id,
                    previousInstance.CurrentStageOrder,
                    Stages = previousInstance.Stages.Select(s => new
                    {
                        s.StageOrder,
                        s.StageName,
                        s.Status,
                        s.ApprovedByUserId,
                        s.ApprovedAt,
                        s.RejectedByUserId,
                        s.RejectedAt,
                        s.RejectionReason,
                    }),
                });

                await auditLogWriter.LogAsync(
                    action: "DELETE",
                    tableName: "workflow_instances",
                    recordId: previousInstance.Id,
                    userId: userId,
                    companyId: plan.CompanyId,
                    oldValues: snapshot,
                    ct: ct
                );
            }

            await workflowRepo.DeleteInstanceAsync(plan.WorkflowInstanceId.Value, ct);
        }

        await recapRepo.ResetToPendingByBudgetPlanIdAsync(plan.Id, ct);

        var instance = new WorkflowInstance
        {
            WorkflowTemplateId = workflowTemplate.Id,
            DocType = DocType,
            DocId = plan.Id,
            CurrentStageOrder = 1,
            Stages = [.. stages.Select(s => new WorkflowInstanceStage
            {
                StageOrder = s.StageOrder,
                StageName = s.StageName,
                ApproverRoles = s.ApproverRoles,
                Status = WorkflowStageStatus.Pending,
            })],
        };

        plan.WorkflowInstance = instance;
        plan.RejectedByUserId = null;
        plan.RejectedAt = null;
        plan.RejectionReason = null;
        plan.Status = BudgetPlanStatus.Submitted;
        plan.SubmittedAt = DateTime.UtcNow;
        plan.SubmittedByUserId = userId;
        plan.UpdatedAt = DateTime.UtcNow;

        await workflowRepo.CreateInstanceAsync(instance, ct);
        await budgetPlanRepo.UpdateAsync(plan, ct);
        await uow.CommitAsync(ct);
    }

    // In Super Admin bypass mode the tenant query filter on WarehouseShadow is disabled, so
    // warehouseRepo.GetByIdAsync can return a warehouse from any company. Nothing else here
    // compares that warehouse's company against the plan's own tenant, so without this check a
    // budget plan can end up pointing at another company's warehouse entirely.
    private void EnsureWarehouseBelongsToTenant(WarehouseShadow warehouse)
    {
        var effectiveCompanyId = tenantContext.CompanyId;
        if (effectiveCompanyId.HasValue && warehouse.CompanyId != effectiveCompanyId.Value)
            throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouse.Id));
    }

    // SPK attachments are scoped to every warehouse the user can access, not just whichever
    // warehouse happens to be "currently active" in the FE's X-Warehouse-Id header - that
    // header is a browsing-view narrower (used by SpkService's list/get-by-id), but a budget
    // plan submit can legitimately reference SPKs from any warehouse the user has access to,
    // regardless of what's currently selected elsewhere in the UI.
    private async Task<IReadOnlyList<string>?> ResolveSpkWhsCodesAsync(long userId, CancellationToken ct)
    {
        var hasGlobal = await rbacService.HasGlobalAccessAsync(userId, ct);
        if (hasGlobal)
            return null;

        var warehouseIds = await userRepo.GetUserWarehouseIdsAsync(userId, ct);
        return await warehouseRepo.GetCodesByIdsAsync(warehouseIds, ct);
    }

    private async Task EnsureWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        var (exists, hasAccess) = await userRepo.CheckWarehouseAccessAsync(userId, warehouseId, ct);

        if (!exists)
            throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));

        if (!hasAccess)
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }

    private async Task AddItemsAsync(
        BudgetPlan plan,
        List<CreateBudgetPlanItemRequest> items,
        IReadOnlyDictionary<long, (string ItemCode, decimal? Quantity)> spkMap,
        CancellationToken ct
    )
    {
        // Batch vendor existence check
        var vendorIds = items.Select(i => i.VendorShadowId).Distinct().ToList();
        var foundVendors = await vendorRepo.GetByIdsAsync(vendorIds, ct);
        var foundVendorIds = foundVendors.Select(v => v.Id).ToHashSet();
        var missingVendor = vendorIds.FirstOrDefault(id => !foundVendorIds.Contains(id));
        if (missingVendor != 0)
            throw new NotFoundException(ErrorMessages.Vendor.NotFound(missingVendor));

        // Batch rate card lookup: one query for all (vendor, item) pairs
        var pairs = items.Select(i => (i.VendorShadowId, i.ItemShadowId)).ToList();
        var rateMap = await rateCardRepo.FindSubmittedRatesBatchAsync(pairs, ct);

        // Batch-validate any explicit UomMasterId overrides in one query
        var overrideUomIds = items
            .Where(i => i.UomMasterId.HasValue)
            .Select(i => i.UomMasterId!.Value)
            .Distinct()
            .ToList();
        HashSet<long> validUomIds = [];
        if (overrideUomIds.Count > 0)
        {
            var foundUoms = await uomRepo.GetByIdsAsync(overrideUomIds, ct);
            validUomIds = [.. foundUoms.Select(u => u.Id)];
            var missingUom = overrideUomIds.FirstOrDefault(id => !validUomIds.Contains(id));
            if (missingUom != 0)
                throw new NotFoundException(ErrorMessages.Uom.NotFound(missingUom));
        }

        // Reject cost items that reference an SPK not linked to this plan.
        // Also enforce quantity ceiling from the linked SPK row.
        var itemsWithSpk = items.Where(i => i.SpkShadowId.HasValue).ToList();
        foreach (var req in itemsWithSpk)
        {
            if (!spkMap.TryGetValue(req.SpkShadowId!.Value, out var spkEntry))
                throw new ValidationException(ErrorMessages.Spk.NotLinkedToPlan(req.SpkShadowId.Value));

            // A zero SPK quantity is never a meaningful cap (true for BL rows synced with no
            // real quantity yet, and equally true for any SPK with a data-entry gap) - treat it
            // the same as null and skip the check, rather than blocking every input.
            if (spkEntry.Quantity is > 0 && req.Quantity > spkEntry.Quantity.Value)
                throw new ValidationException(
                    ErrorMessages.Spk.QuantityExceedsSpk(req.Quantity, spkEntry.Quantity.Value, req.SpkShadowId.Value));
        }

        // Batch activity type existence check
        var explicitAtIds = items
            .Select(i => i.ActivityTypeId)
            .Distinct()
            .ToList();

        if (explicitAtIds.Count > 0)
        {
            var foundAtIds = (await activityTypeRepo.GetByIdsAsync(explicitAtIds, ct))
                .Select(a => a.Id)
                .ToHashSet();

            var missingAt = explicitAtIds.FirstOrDefault(id => !foundAtIds.Contains(id));

            if (missingAt != default) throw new NotFoundException(ErrorMessages.ActivityType.NotFound(missingAt));
        }

        for (var i = 0; i < items.Count; i++)
        {
            var req = items[i];

            if (!rateMap.TryGetValue((req.VendorShadowId, req.ItemShadowId), out var rateItem))
            {
                var diagnostics = await rateCardRepo.GetRateAvailabilityDiagnosticsAsync(
                    [(req.VendorShadowId, req.ItemShadowId)], ct);
                var reason = diagnostics.FirstOrDefault();

                throw new NotFoundException(reason switch
                {
                    { Found: false } => ErrorMessages.RateCard.RateCardNotFoundForVendorItem(req.VendorShadowId, req.ItemShadowId),
                    { Submitted: false } => ErrorMessages.RateCard.RateCardNotSubmitted(req.VendorShadowId, req.ItemShadowId),
                    _ => ErrorMessages.RateCard.SubmittedRateNotFound(req.VendorShadowId, req.ItemShadowId),
                });
            }

            if (req.CostValue.HasValue && req.CostValue.Value <= 0)
                throw new ValidationException(ErrorMessages.BudgetPlan.UnitCostOverrideMustBePositive(req.ItemShadowId));

            var costValue = req.CostValue ?? rateItem.CostValue;
            var uomMasterId = req.UomMasterId ?? rateItem.UomMasterId;
            var totalValue = costValue * req.Quantity;
            var ppnRate = rateItem.PpnRate ?? 0m;
            var pphRate = rateItem.PphRate ?? 0m;
            var tax = TaxCalculator.Calculate(totalValue, ppnRate, pphRate);

            plan.Items.Add(new BudgetPlanItem
            {
                ItemShadowId = req.ItemShadowId,
                ActivityTypeId = req.ActivityTypeId,
                VendorShadowId = req.VendorShadowId,
                UomMasterId = uomMasterId,
                Type = req.Type,
                IsRfba = req.IsRfba,
                CostValue = costValue,
                Quantity = req.Quantity,
                TotalValue = totalValue,
                SortOrder = i + 1,
                BillOfLading = req.BillOfLading,
                Description = req.Description,
                SpkShadowId = req.SpkShadowId,
                PpnTaxTypeCode = rateItem.PpnTaxTypeCode,
                PpnRate = ppnRate,
                PphTaxTypeCode = rateItem.PphTaxTypeCode,
                PphRate = pphRate,
                PpnAmount = tax.PpnAmount,
                PphAmount = tax.PphAmount,
                GrandTotal = tax.GrandTotal,
                CostTreatment = rateItem.CostTreatment,
            });
        }
    }

    private async Task<IReadOnlyDictionary<long, (string ItemCode, decimal? Quantity)>> AddSpkItemsAsync(
        BudgetPlan plan,
        List<long>? spkShadowIds,
        long userId,
        CancellationToken ct
    )
    {
        if (spkShadowIds is null || spkShadowIds.Count == 0)
            return new Dictionary<long, (string, decimal?)>();

        var seen = new HashSet<long>();
        var uniqueIds = spkShadowIds.Where(id => seen.Add(id)).ToList();

        var whsCodes = await ResolveSpkWhsCodesAsync(userId, ct);
        var spks = await spkRepo.GetByIdsAsync(uniqueIds, whsCodes, ct);
        var spkById = spks.ToDictionary(s => s.Id);

        var missingId = uniqueIds.FirstOrDefault(id => !spkById.ContainsKey(id));
        if (missingId != 0)
            throw new NotFoundException(ErrorMessages.Spk.NotFound(missingId));

        for (var i = 0; i < uniqueIds.Count; i++)
        {
            plan.SpkItems.Add(new BudgetPlanSpkItem
            {
                SpkShadowId = uniqueIds[i],
                SortOrder = i + 1,
            });
        }

        return spkById.ToDictionary(kv => kv.Key, kv => (kv.Value.ItemCode, kv.Value.Quantity));
    }

    public async Task<BudgetPlanSpkItemResponse> AddSpkItemAsync(
        long planId,
        AddSpkItemRequest request,
        long userId,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdWithItemsAsync(planId, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(planId));

        var whsCodes = await ResolveSpkWhsCodesAsync(userId, ct);
        var spk = await spkRepo.GetByIdAsync(request.SpkShadowId, whsCodes, ct)
            ?? throw new NotFoundException(ErrorMessages.Spk.NotFound(request.SpkShadowId));

        if (plan.SpkItems.Any(s => s.SpkShadowId == request.SpkShadowId))
            throw new ConflictException(ErrorMessages.Spk.AlreadyLinked(spk.DocNo));

        var sortOrder = plan.SpkItems.Count == 0 ? 1 : plan.SpkItems.Max(s => s.SortOrder) + 1;

        var item = new BudgetPlanSpkItem
        {
            BudgetPlanId = planId,
            SpkShadowId = request.SpkShadowId,
            SortOrder = sortOrder,
        };
        plan.SpkItems.Add(item);
        await uow.CommitAsync(ct);

        var itemShadowId = await itemShadowRepo.GetIdByItemCodeAsync(spk.ItemCode, ct);

        return new BudgetPlanSpkItemResponse(
            item.Id,
            spk.Id,
            spk.Type,
            spk.DocNo,
            spk.BaseDoc,
            spk.BaseDocNo,
            spk.CardCode,
            spk.CardName,
            spk.ItemCode,
            spk.ItemName,
            spk.Quantity,
            spk.DeliveryQty,
            spk.UoM,
            spk.PackType,
            spk.WhsCode,
            spk.WhsName,
            spk.DocStatus,
            spk.BlNo,
            item.SortOrder,
            itemShadowId
        );
    }

    public async Task RemoveSpkItemAsync(
        long planId,
        long spkItemId,
        CancellationToken ct = default
    )
    {
        var plan = await budgetPlanRepo.GetByIdWithItemsAsync(planId, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetPlan.NotFound(planId));

        var item = plan.SpkItems.FirstOrDefault(s => s.Id == spkItemId)
            ?? throw new NotFoundException(ErrorMessages.Spk.ItemNotFound(spkItemId, planId));

        plan.SpkItems.Remove(item);
        await uow.CommitAsync(ct);
    }

    private static BudgetPlanApprovalInfo MapApprovalInfo(BudgetPlan plan)
    {
        if (plan.WorkflowInstance is null)
            return new BudgetPlanApprovalInfo(0, 0, []);

        var stages = plan.WorkflowInstance.Stages
            .OrderBy(s => s.StageOrder)
            .Select(s => new WorkflowStageInfo(
                s.StageOrder,
                s.StageName,
                s.ApproverRoles,
                s.Status,
                s.ApprovedAt,
                s.ApprovedBy?.Fullname,
                s.RejectedAt,
                s.RejectedBy?.Fullname,
                s.RejectionReason))
            .ToList();

        return new BudgetPlanApprovalInfo(
            stages.Count,
            plan.WorkflowInstance.CurrentStageOrder,
            stages
        );
    }

    private static BudgetPlanResponse MapDetail(BudgetPlan p) => new(
        p.Id,
        p.Code,
        new BudgetTemplateSummaryInfo(
            p.BudgetTemplate.Id,
            p.BudgetTemplate.Code,
            p.BudgetTemplate.ProvinceId,
            p.BudgetTemplate.Province?.Name,
            p.BudgetTemplate.Province?.Display
        ),
        p.Warehouse.Code,
        p.Warehouse.Name,
        p.Remark,
        p.DocDate,
        p.Status.ToString(),
        p.Status.DisplayName,
        [.. p.SpkItems.OrderBy(s => s.SortOrder).Select(s => new BudgetPlanSpkItemResponse(
            s.Id,
            s.Spk.Id,
            s.Spk.Type,
            s.Spk.DocNo,
            s.Spk.BaseDoc,
            s.Spk.BaseDocNo,
            s.Spk.CardCode,
            s.Spk.CardName,
            s.Spk.ItemCode,
            s.Spk.ItemName,
            s.Spk.Quantity,
            s.Spk.DeliveryQty,
            s.Spk.UoM,
            s.Spk.PackType,
            s.Spk.WhsCode,
            s.Spk.WhsName,
            s.Spk.DocStatus,
            s.Spk.BlNo,
            s.SortOrder,
            null))],
        [.. p.Items.OrderBy(i => i.SortOrder).Select(i => new BudgetPlanItemResponse(
            i.Id,
            i.ItemShadowId,
            i.Item.ItemCode,
            i.Item.ItemName,
            i.Item.AcctCode,
            i.Item.AcctName,
            i.VendorShadowId,
            i.Vendor.CardCode,
            i.Vendor.CardName,
            i.UomMasterId,
            i.Uom.Code,
            i.Uom.Name,
            i.CostValue,
            i.Quantity,
            i.TotalValue,
            i.SortOrder,
            i.Type.ToString(),
            i.IsRfba,
            i.DocExternal,
            i.BillOfLading,
            i.Description,
            i.ActivityTypeId,
            i.ActivityType?.Code,
            i.ActivityType?.Name,
            i.SpkShadowId,
            i.PpnTaxTypeCode,
            i.PpnRate,
            i.PphTaxTypeCode,
            i.PphRate,
            i.PpnAmount,
            i.PphAmount,
            i.GrandTotal,
            i.CostTreatment))],
        p.Items.Sum(i => i.TotalValue),
        p.Items.Sum(i => i.PpnAmount),
        p.Items.Sum(i => i.PphAmount),
        p.Items.Sum(i => i.GrandTotal),
        p.CreatedAt,
        p.CreatedBy.Fullname,
        p.SubmittedAt,
        p.SubmittedBy?.Fullname,
        MapApprovalInfo(p),
        p.RejectedAt,
        p.RejectedBy?.Fullname,
        p.RejectionReason
    );

    private async Task<IEnumerable<NotificationCreateRequest>> BuildApprovalNotificationsAsync(
        BudgetPlan plan,
        WorkflowInstance instance,
        WorkflowInstanceStage approvedStage,
        long actorUserId,
        CancellationToken ct
    )
    {
        var notifications = new List<NotificationCreateRequest>();

        if (plan.Status == BudgetPlanStatus.Approved)
        {
            if (plan.CreatedByUserId != actorUserId)
            {
                notifications.Add(new NotificationCreateRequest(
                    plan.CompanyId,
                    plan.CreatedByUserId,
                    actorUserId,
                    "budget_plan_approved_final",
                    "Budget Plan Fully Approved",
                    $"Budget plan {plan.Code} has completed all approval stages.",
                    "budget_plan",
                    plan.Id.ToString()));
            }
        }
        else
        {
            if (plan.CreatedByUserId != actorUserId)
            {
                notifications.Add(new NotificationCreateRequest(
                    plan.CompanyId,
                    plan.CreatedByUserId,
                    actorUserId,
                    "budget_plan_stage_approved",
                    $"Budget Plan Approved - Stage {approvedStage.StageOrder}",
                    $"Budget plan {plan.Code} passed stage {approvedStage.StageOrder} approval ({approvedStage.StageName}).",
                    "budget_plan",
                    plan.Id.ToString()));
            }

            var nextStage = instance.Stages
                .FirstOrDefault(s => s.StageOrder == instance.CurrentStageOrder);

            if (nextStage is not null)
            {
                var nextApprovers = await userRepo.GetUsersByRolesAndWarehouseAsync(
                    plan.CompanyId,
                    plan.WarehouseShadowId,
                    new HashSet<string>(nextStage.ApproverRoles, StringComparer.OrdinalIgnoreCase),
                    ct);

                notifications.AddRange(nextApprovers
                    .Where(u => u.Id != actorUserId)
                    .Select(u => new NotificationCreateRequest(
                        plan.CompanyId,
                        u.Id,
                        actorUserId,
                        "budget_plan_pending_approval",
                        "Budget Plan Waiting for Approval",
                        $"Budget plan {plan.Code} is waiting for your stage {nextStage.StageOrder} approval ({nextStage.StageName}).",
                        "budget_plan",
                        plan.Id.ToString())));
            }
        }

        return notifications;
    }

    private IEnumerable<NotificationCreateRequest> BuildRejectNotifications(BudgetPlan plan, long actorUserId)
    {
        if (plan.CreatedByUserId == actorUserId)
            return [];

        return
        [
            new NotificationCreateRequest(
                plan.CompanyId,
                plan.CreatedByUserId,
                actorUserId,
                "budget_plan_rejected",
                "Budget Plan Rejected",
                $"Budget plan {plan.Code} has been rejected.",
                "budget_plan",
                plan.Id.ToString())
        ];
    }

    private async Task TryPublishNotificationsAsync(
        IEnumerable<NotificationCreateRequest> notifications,
        CancellationToken ct
    )
    {
        try
        {
            await notificationService.PublishAsync(notifications, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish budget plan approval notifications");
        }
    }
}
