namespace WAMS.Application.DTOs.TaxTypes;

using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Entities.TaxTypes;

public record TaxTypeResponse(long Id, string Category, string Code, string Name, decimal Rate, bool IsActive)
{
    // rateOverride is the snapshot rate frozen on the referencing item (RateCardItem.PpnRate/PphRate),
    // which takes precedence over the TaxType's own current rate.
    public static TaxTypeResponse From(TaxType taxType, decimal? rateOverride = null) =>
        new(
            taxType.Id,
            taxType.Category.Value,
            taxType.Code,
            taxType.Name,
            rateOverride ?? taxType.Rate,
            taxType.IsActive
        );
}
