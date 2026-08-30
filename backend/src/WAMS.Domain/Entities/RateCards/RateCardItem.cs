namespace WAMS.Domain.Entities.RateCards;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.TaxTypes;
using WAMS.Domain.Entities.Uoms;

public class RateCardItem : BaseEntity
{
    public long RateCardId { get; set; }
    public long ItemShadowId { get; set; }
    public long UomMasterId { get; set; }
    public decimal CostValue { get; set; }

    // Tax snapshot: a frozen point-in-time copy of the selected TaxType. There is NO live FK -
    // deactivating, renaming, or re-rating the TaxType never affects rows already snapshotted here.
    // The id is kept as a plain column (not a foreign key) purely so the edit form can round-trip
    // the current selection; the code is stored so reads render without joining tax_types.
    public long? PpnTaxTypeId { get; set; }
    public string? PpnTaxTypeCode { get; set; }
    public decimal? PpnRate { get; set; }
    public long? PphTaxTypeId { get; set; }
    public string? PphTaxTypeCode { get; set; }
    public decimal? PphRate { get; set; }

    // Per-line cost treatment label (Dibiayakan / TidakDibiayakan). Governs both PPN and PPh.
    // Label only - does not affect any computed total. Values come from WAMS.Domain.Constants.CostTreatments.
    public string? CostTreatment { get; set; }

    public RateCard RateCard { get; set; } = null!;
    public ItemShadow Item { get; set; } = null!;
    public UomMaster Uom { get; set; } = null!;
}
