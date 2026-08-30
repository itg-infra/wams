namespace WAMS.Application.Services.BudgetTemplates;

using WAMS.Application.Common;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

public class BudgetTemplateService(
    IBudgetTemplateRepository budgetTemplateRepo,
    IActivityTypeRepository activityTypeRepo,
    IItemShadowRepository itemRepo,
    IUnitOfWork uow,
    IProvinceRepository provinceRepo,
    IRbacService rbacService,
    IUserService userService,
    ICodeCounterRepository codeCounterRepo
) : IBudgetTemplateService
{
    public async Task<(List<BudgetTemplateSummaryResponse> Items, int TotalCount)> GetAllAsync(
        BudgetTemplateStatus? status,
        BudgetTemplateQuery query,
        long userId,
        CancellationToken ct = default
    )
    {
        List<long>? provinceFilter = null;

        if (!await rbacService.HasGlobalAccessAsync(userId, ct))
            provinceFilter = await userService.GetUserProvinceIdsAsync(userId, ct);

        var (items, total) = await budgetTemplateRepo.GetAllAsync(status, query, provinceFilter, ct);

        var data = items.Select(t => new BudgetTemplateSummaryResponse(
            t.Id,
            t.Code,
            t.ProvinceId,
            t.Province?.Name,
            t.Province?.Display,
            t.CreatedAt,
            t.Status.ToString())).ToList();

        return (data, total);
    }

    public async IAsyncEnumerable<BudgetTemplateSummaryResponse> StreamAllAsync(
        BudgetTemplateStatus? status,
        BudgetTemplateQuery query,
        long userId,
        int limit,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default
    )
    {
        List<long>? provinceFilter = null;

        if (!await rbacService.HasGlobalAccessAsync(userId, ct))
            provinceFilter = await userService.GetUserProvinceIdsAsync(userId, ct);

        await foreach (
            var item in budgetTemplateRepo.StreamAllAsync(
                status,
                query,
                provinceFilter,
                limit,
                ct
            ).WithCancellation(ct)
        )

            yield return item;
    }

    public async Task<BudgetTemplateResponse> GetByIdAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var t = await budgetTemplateRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFound(id));

        return MapDetail(t);
    }

    public async Task<BudgetTemplateResponse> CreateAsync(
        long userId,
        CreateBudgetTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var template = await BuildTemplateAsync(userId, request, BudgetTemplateStatus.Draft, ct);

        await budgetTemplateRepo.CreateAsync(template, ct);
        await uow.CommitAsync(ct);

        var created = await budgetTemplateRepo.GetByIdWithItemsAsync(template.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFoundAfterCreation);

        return MapDetail(created);
    }

    public async Task<BudgetTemplateResponse> CreateAndSubmitAsync(
        long userId,
        CreateBudgetTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var template = await BuildTemplateAsync(userId, request, BudgetTemplateStatus.Submitted, ct);
        template.SubmittedAt = DateTime.UtcNow;
        template.SubmittedByUserId = userId;

        await budgetTemplateRepo.CreateAsync(template, ct);
        await uow.CommitAsync(ct);

        var created = await budgetTemplateRepo.GetByIdWithItemsAsync(template.Id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFoundAfterCreation);

        return MapDetail(created);
    }

    private async Task<BudgetTemplate> BuildTemplateAsync(
        long userId,
        CreateBudgetTemplateRequest request,
        BudgetTemplateStatus status,
        CancellationToken ct
    )
    {
        var seenItemIds = new HashSet<long>();

        foreach (var item in request.Items)
            if (!seenItemIds.Add(item.ItemShadowId))
                throw new ValidationException(ErrorMessages.Item.DuplicateShadow(item.ItemShadowId));

        var foundItems = await itemRepo.GetByIdsAsync(seenItemIds, ct);
        var foundItemIds = foundItems.Select(i => i.Id).ToHashSet();
        var missingItemId = seenItemIds.FirstOrDefault(id => !foundItemIds.Contains(id));

        if (missingItemId != 0)
            throw new NotFoundException(ErrorMessages.Item.ShadowNotFound(missingItemId));

        await ValidateItemActivityTypesAsync(request.Items, ct);

        if (request.ProvinceId.HasValue) await ValidateProvinceAsync(request.ProvinceId.Value, ct);

        var prefix = $"BT-{DateTime.UtcNow:yyMM}";
        var code = await DocumentCodeGenerator.NextCodeAsync(codeCounterRepo, prefix, ct);

        var template = new BudgetTemplate
        {
            Code = code,
            ProvinceId = request.ProvinceId,
            Status = status,
            CreatedByUserId = userId,
        };

        for (var i = 0; i < request.Items.Count; i++)
        {
            template.Items.Add(new BudgetTemplateItem
            {
                ItemShadowId = request.Items[i].ItemShadowId,
                ActivityTypeId = request.Items[i].ActivityTypeId,
                SortOrder = i + 1,
            });
        }

        return template;
    }

    public async Task<BudgetTemplateResponse> UpdateAsync(
        long id,
        UpdateBudgetTemplateRequest request,
        CancellationToken ct = default
    )
    {
        var template = await budgetTemplateRepo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFound(id));

        if (!template.Status.CanBeEdited)
            throw new ValidationException(ErrorMessages.BudgetTemplate.CannotUpdateOnlyDraftOrSubmitted);

        if (request.ProvinceId.HasValue)
        {
            await ValidateProvinceAsync(request.ProvinceId.Value, ct);
            template.ProvinceId = request.ProvinceId.Value;
        }

        if (request.Items is not null)
        {
            var seenItemIds = new HashSet<long>();

            foreach (var item in request.Items)
                if (!seenItemIds.Add(item.ItemShadowId))
                    throw new ValidationException(ErrorMessages.Item.DuplicateShadow(item.ItemShadowId));

            var foundItems = await itemRepo.GetByIdsAsync(seenItemIds, ct);
            var foundItemIds = foundItems.Select(i => i.Id).ToHashSet();
            var missingItemId = seenItemIds.FirstOrDefault(id => !foundItemIds.Contains(id));

            if (missingItemId != 0)
                throw new NotFoundException(ErrorMessages.Item.ShadowNotFound(missingItemId));

            await ValidateItemActivityTypesAsync(request.Items, ct);

            template.Items.Clear();

            for (var i = 0; i < request.Items.Count; i++)
            {
                template.Items.Add(new BudgetTemplateItem
                {
                    ItemShadowId = request.Items[i].ItemShadowId,
                    ActivityTypeId = request.Items[i].ActivityTypeId,
                    SortOrder = i + 1,
                });
            }
        }

        template.UpdatedAt = DateTime.UtcNow;

        await budgetTemplateRepo.UpdateAsync(template, ct);
        await uow.CommitAsync(ct);

        var updated = await budgetTemplateRepo.GetByIdWithItemsAsync(id, ct)!;

        return MapDetail(updated!);
    }

    public async Task SubmitAsync(
        long id,
        long userId,
        CancellationToken ct = default
    )
    {
        var template = await budgetTemplateRepo.GetTrackedAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFound(id));

        if (!template.Status.CanBeSubmitted)
            throw new ValidationException(ErrorMessages.BudgetTemplate.CannotSubmitOnlyDraft);

        template.Status = BudgetTemplateStatus.Submitted;
        template.SubmittedAt = DateTime.UtcNow;
        template.SubmittedByUserId = userId;
        template.UpdatedAt = DateTime.UtcNow;

        await budgetTemplateRepo.UpdateAsync(template, ct);
        await uow.CommitAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var template = await budgetTemplateRepo.GetTrackedAsync(id, ct)
            ?? throw new NotFoundException(ErrorMessages.BudgetTemplate.NotFound(id));

        if (!template.Status.CanBeDeleted)
            throw new ValidationException(ErrorMessages.BudgetTemplate.CannotDeleteOnlyDraft);

        await budgetTemplateRepo.SoftDeleteAsync(id, ct);
        await uow.CommitAsync(ct);
    }

    private async Task ValidateItemActivityTypesAsync(
        List<CreateBudgetTemplateItemRequest> items,
        CancellationToken ct
    )
    {
        var missingActivityType = items.FirstOrDefault(i => i.ActivityTypeId <= 0);
        if (missingActivityType is not null)
            throw new ValidationException(ErrorMessages.Validation.BudgetPlan.ActivityTypeRequired);

        var distinctIds = items
            .Select(i => i.ActivityTypeId)
            .Distinct()
            .ToList();

        if (distinctIds.Count == 0) return;

        var found = await activityTypeRepo.GetByIdsAsync(distinctIds, ct);
        var foundMap = found.ToDictionary(a => a.Id);

        foreach (var item in items)
        {
            if (!foundMap.TryGetValue(item.ActivityTypeId, out var at))
                throw new NotFoundException(ErrorMessages.ActivityType.NotFound(item.ActivityTypeId));
            if (!at.IsActive)
                throw new ValidationException(ErrorMessages.ActivityType.NotActive(at.Name));
        }
    }

    private async Task ValidateProvinceAsync(long provinceId, CancellationToken ct)
    {
        var province = await provinceRepo.GetByIdAsync(provinceId, ct)
            ?? throw new NotFoundException(ErrorMessages.Province.NotFound(provinceId));

        if (!province.IsActive) throw new ValidationException(ErrorMessages.Province.NotActive(province.Name));
    }

    private static BudgetTemplateResponse MapDetail(BudgetTemplate t)
        => new(
            t.Id,
            t.Code,
            t.ProvinceId,
            t.Province?.Name,
            t.Province?.Display,
            t.Status.ToString(),
            [.. t.Items.OrderBy(i => i.SortOrder).Select(i => new BudgetTemplateItemResponse(
                i.Id,
                i.ItemShadowId,
                i.Item.ItemCode,
                i.Item.ItemName,
                i.Item.AcctCode,
                i.Item.AcctName,
                i.SortOrder,
                i.ActivityTypeId,
                i.ActivityType?.Code,
                i.ActivityType?.Name))],
            t.CreatedAt,
            t.CreatedBy.Fullname,
            t.SubmittedAt,
            t.SubmittedBy?.Fullname
        );
}
