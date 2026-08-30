namespace WAMS.Domain.Entities.Vendors;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.TaxTypes;

public class VendorPphAssignment : BaseEntity
{
    public long VendorShadowId { get; set; }
    public long TaxTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public VendorShadow Vendor { get; set; } = null!;
    public TaxType TaxType { get; set; } = null!;
}
