namespace WAMS.Application.Interfaces.AccountPayables;

using WAMS.Application.DTOs.AccountPayables;

public interface IAccountPayableService
{
    Task<(List<AccountPayableSummaryResponse> Items, int TotalCount)> GetAllAsync(AccountPayableQuery q, CancellationToken ct = default);
    IAsyncEnumerable<AccountPayableSummaryResponse> StreamAllAsync(AccountPayableQuery q, int limit, CancellationToken ct = default);
    Task<AccountPayableResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(List<ApprovedRecapApStatusResponse> Items, int Total)> GetApprovedRecapsAsync(long userId, int page, int limit, CancellationToken ct = default);
    Task<List<AvailableApItemResponse>> GetAvailableItemsByBudgetPlansAsync(long userId, long vendorShadowId, List<long> budgetPlanIds, bool includeGenerated = false, long? excludeAccountPayableId = null, CancellationToken ct = default);
    Task<AccountPayableTotalsResponse> PreviewAsync(long userId, PreviewAccountPayableRequest request, CancellationToken ct = default);
    Task<AccountPayableResponse> CreateAsync(long userId, CreateAccountPayableRequest request, CancellationToken ct = default);
    Task<AccountPayableResponse> CreateAndGenerateAsync(long userId, CreateAccountPayableRequest request, CancellationToken ct = default);
    Task<AccountPayableResponse> UpdateAsync(long id, long userId, UpdateAccountPayableRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<AccountPayableResponse> GenerateAsync(long id, long userId, CancellationToken ct = default);
}
