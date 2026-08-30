namespace WAMS.Application.Interfaces.Companies;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Companies;
using WAMS.Application.Common;

public interface ICompanyService
{
    Task<CompanyResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PaginatedResponse<CompanyResponse>> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<CompanyResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<List<CompanyPublicResponse>> GetActivePublicAsync(string? code = null, CancellationToken ct = default);
    Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default);
    Task<CompanyResponse> UpdateAsync(long id, UpdateCompanyRequest request, CancellationToken ct = default);
    Task DeactivateAsync(long id, CancellationToken ct = default);
    Task AssignUserToCompanyAsync(long userId, long companyId, CancellationToken ct = default);
    Task<(Stream Content, string ContentType)> GetLogoAsync(long companyId, CancellationToken ct = default);
    Task UploadLogoAsync(long companyId, Stream content, string contentType, CancellationToken ct = default);
    Task RemoveLogoAsync(long companyId, CancellationToken ct = default);
}
