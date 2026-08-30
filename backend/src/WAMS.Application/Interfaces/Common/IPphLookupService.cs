namespace WAMS.Application.Interfaces.Common;

using WAMS.Application.DTOs.TaxTypes;

public interface IPphLookupService
{
    Task<List<TaxTypeResponse>> GetOrRefreshAsync(long vendorShadowId, CancellationToken ct = default);
}
