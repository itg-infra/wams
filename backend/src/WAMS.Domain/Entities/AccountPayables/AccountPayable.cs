namespace WAMS.Domain.Entities.AccountPayables;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Enums;

public class AccountPayable : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long VendorShadowId { get; set; }
    public string? Remark { get; set; }
    public DateTime DocDate { get; set; }
    public AccountPayableStatus Status { get; set; } = AccountPayableStatus.Draft;
    public string? SapApNumber { get; set; }
    public int? SapDocEntry { get; set; }
    public DateTime? GenerationClaimedAt { get; set; }
    public string? GenerationClaimToken { get; set; }
    public decimal DiscountAmount { get; set; }

    public long CreatedByUserId { get; set; }
    public long? GeneratedByUserId { get; set; }
    public DateTime? GeneratedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public VendorShadow Vendor { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? GeneratedBy { get; set; }
    public ICollection<AccountPayableItem> Items { get; set; } = [];
}
