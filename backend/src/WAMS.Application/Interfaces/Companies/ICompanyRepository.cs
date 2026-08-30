namespace WAMS.Application.Interfaces.Companies;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Domain.Entities.Companies;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<CompanyResponse?> GetByIdWithCountsAsync(long id, CancellationToken ct = default);
    Task<Company?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<(List<CompanyResponse> Items, int TotalCount)> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<CompanyResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<List<Company>> GetActiveAsync(string? code = null, CancellationToken ct = default);
    Task<Company> CreateAsync(Company company, CancellationToken ct = default);
    Task UpdateAsync(Company company, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
}
