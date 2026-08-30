namespace WAMS.Domain.Entities.RateCards;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;

public class RateCard : BaseEntity
{
    public long CompanyId { get; set; }
    public long VendorShadowId { get; set; }
    public RateCardStatus Status { get; set; } = RateCardStatus.Draft;
    public long CreatedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public VendorShadow Vendor { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<RateCardItem> Items { get; set; } = [];
}
