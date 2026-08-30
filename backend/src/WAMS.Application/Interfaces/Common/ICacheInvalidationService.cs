namespace WAMS.Application.Interfaces.Common;

public interface ICacheInvalidationService
{
    Task InvalidateWarehouseShadowsAsync(CancellationToken ct = default);
    Task InvalidateWarehouseShadowsForUserAsync(long userId, CancellationToken ct = default);
    Task InvalidateRateCardsAsync(CancellationToken ct = default);
    Task InvalidateTaxTypesAsync(CancellationToken ct = default);
}
