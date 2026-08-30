namespace WAMS.Application.Interfaces.BudgetTemplates;

using WAMS.Application.Common;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Domain.Enums;

public interface IBudgetTemplateService
{
    Task<(List<BudgetTemplateSummaryResponse> Items, int TotalCount)> GetAllAsync(
        BudgetTemplateStatus? status, 
        BudgetTemplateQuery query, 
        long userId, 
        CancellationToken ct = default
    );

    IAsyncEnumerable<BudgetTemplateSummaryResponse> StreamAllAsync(
        BudgetTemplateStatus? status, 
        BudgetTemplateQuery query, 
        long userId, 
        int limit, 
        CancellationToken ct = default
    );

    Task<BudgetTemplateResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default);
    Task<BudgetTemplateResponse> CreateAsync(long userId, CreateBudgetTemplateRequest request, CancellationToken ct = default);
    Task<BudgetTemplateResponse> CreateAndSubmitAsync(long userId, CreateBudgetTemplateRequest request, CancellationToken ct = default);
    Task<BudgetTemplateResponse> UpdateAsync(long id, UpdateBudgetTemplateRequest request, CancellationToken ct = default);
    Task SubmitAsync(long id, long userId, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
