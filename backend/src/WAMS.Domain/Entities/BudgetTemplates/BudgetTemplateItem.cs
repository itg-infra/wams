namespace WAMS.Domain.Entities.BudgetTemplates;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.Items;

public class BudgetTemplateItem : BaseEntity
{
    public long BudgetTemplateId { get; set; }
    public long ItemShadowId { get; set; }
    public long ActivityTypeId { get; set; }
    public int SortOrder { get; set; }

    public BudgetTemplate BudgetTemplate { get; set; } = null!;
    public ItemShadow Item { get; set; } = null!;
    public ActivityType? ActivityType { get; set; }
}
