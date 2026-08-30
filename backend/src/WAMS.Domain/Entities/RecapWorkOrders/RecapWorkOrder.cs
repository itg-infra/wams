namespace WAMS.Domain.Entities.RecapWorkOrders;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Enums;

public class RecapWorkOrder : BaseEntity
{
    public long BudgetPlanId { get; set; }
    public long CompanyId { get; set; }
    public RecapWorkOrderStatus Status { get; set; } = RecapWorkOrderStatus.Pending;
    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    public BudgetPlan BudgetPlan { get; set; } = null!;
    public User? ReviewedBy { get; set; }
}
