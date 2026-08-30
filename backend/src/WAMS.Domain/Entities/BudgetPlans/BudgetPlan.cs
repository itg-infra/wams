namespace WAMS.Domain.Entities.BudgetPlans;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Enums;

public class BudgetPlan : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long BudgetTemplateId { get; set; }
    public long WarehouseShadowId { get; set; }
    public string? Remark { get; set; }
    public DateTime DocDate { get; set; }
    public BudgetPlanStatus Status { get; set; } = BudgetPlanStatus.Draft;

    public long CreatedByUserId { get; set; }
    public long? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public long? WorkflowInstanceId { get; set; }

    public long? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public BudgetTemplate BudgetTemplate { get; set; } = null!;
    public WarehouseShadow Warehouse { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public User? SubmittedBy { get; set; }
    public User? RejectedBy { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }
    public ICollection<BudgetPlanItem> Items { get; set; } = [];
    public ICollection<BudgetPlanSpkItem> SpkItems { get; set; } = [];
    public ICollection<WorkOrder> WorkOrders { get; set; } = [];
}
