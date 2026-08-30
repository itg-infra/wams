namespace WAMS.Domain.Entities.Vendors;

using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;

public class VendorShadow : IShadowEntity
{
    public long Id { get; set; }

    // Tenancy
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // ERP master data fields
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;

    // Shadow table tracking
    public DateTime FirstSeenAt { get; set; }
    public DateTime SyncedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
