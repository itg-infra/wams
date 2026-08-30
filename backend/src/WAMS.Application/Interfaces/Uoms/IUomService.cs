namespace WAMS.Application.Interfaces.Uoms;

using WAMS.Application.DTOs.Uoms;

public interface IUomService
{
    Task<List<UomResponse>> GetAllAsync(bool activeOnly = true, CancellationToken ct = default);
    Task<UomResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UomResponse> CreateAsync(CreateUomRequest request, CancellationToken ct = default);
    Task<UomResponse> UpdateAsync(long id, UpdateUomRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
