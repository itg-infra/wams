namespace WAMS.Domain.Entities.BudgetPlans;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.Spk;

public class BudgetPlanSpkItem : BaseEntity
{
    public long BudgetPlanId { get; set; }
    public long SpkShadowId { get; set; }
    public int SortOrder { get; set; }

    public BudgetPlan BudgetPlan { get; set; } = null!;
    public SpkShadow Spk { get; set; } = null!;
}
