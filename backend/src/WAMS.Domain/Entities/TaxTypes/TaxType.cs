namespace WAMS.Domain.Entities.TaxTypes;

using WAMS.Domain.Common;
using WAMS.Domain.Enums;

public class TaxType : BaseEntity
{
    public long CompanyId { get; set; }
    public TaxCategory Category { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SyncedAt { get; set; }
    public DateTime FirstSeenAt { get; set; }
}
