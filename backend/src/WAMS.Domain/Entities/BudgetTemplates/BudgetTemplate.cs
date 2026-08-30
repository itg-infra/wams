namespace WAMS.Domain.Entities.BudgetTemplates;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Enums;

public class BudgetTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public BudgetTemplateStatus Status { get; set; } = BudgetTemplateStatus.Draft;
    public long CreatedByUserId { get; set; }
    public long? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? SubmittedBy { get; set; }
    public ICollection<BudgetTemplateItem> Items { get; set; } = [];
}
