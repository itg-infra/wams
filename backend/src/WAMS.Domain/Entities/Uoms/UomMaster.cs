namespace WAMS.Domain.Entities.Uoms;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.RateCards;

public class UomMaster : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? DeletedAt { get; set; }

    public ICollection<RateCardItem> RateCardItems { get; set; } = [];
}
