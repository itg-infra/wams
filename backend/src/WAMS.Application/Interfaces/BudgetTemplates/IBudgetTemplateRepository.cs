namespace WAMS.Application.Interfaces.BudgetTemplates;

using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Enums;

public interface IBudgetTemplateRepository
{
    Task<(List<BudgetTemplate> Items, int TotalCount)> GetAllAsync(
        BudgetTemplateStatus? status, 
        BudgetTemplateQuery query, 
        List<long>? provinceFilter = null, 
        CancellationToken ct = default
    );

    IAsyncEnumerable<BudgetTemplateSummaryResponse> StreamAllAsync(
        BudgetTemplateStatus? status, 
        BudgetTemplateQuery query, 
        List<long>? provinceFilter, 
        int limit, 
        CancellationToken ct = default
    );

    Task<BudgetTemplate?> GetByIdWithItemsAsync(long id, CancellationToken ct = default);
    Task<BudgetTemplate?> GetByIdForPlanSourceAsync(long id, CancellationToken ct = default);
    Task<BudgetTemplate?> GetTrackedAsync(long id, CancellationToken ct = default);
    Task CreateAsync(BudgetTemplate template, CancellationToken ct = default);
    Task UpdateAsync(BudgetTemplate template, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
}
