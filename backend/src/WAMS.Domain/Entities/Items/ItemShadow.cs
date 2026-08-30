namespace WAMS.Domain.Entities.Items;

using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;

public class ItemShadow : IShadowEntity
{
    public long Id { get; set; }

    // Tenancy
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    // ERP master data fields
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string AcctCode { get; set; } = string.Empty;
    public string AcctName { get; set; } = string.Empty;

    // Shadow table tracking
    public DateTime FirstSeenAt { get; set; }
    public DateTime SyncedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
