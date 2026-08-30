namespace WAMS.Domain.Entities.WorkOrders;

using WAMS.Domain.Common;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Enums;
using WAMS.Domain.ValueObjects;

public class WorkOrder : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public long BudgetPlanId { get; set; }
    public long? BudgetPlanItemId { get; set; }
    public long ItemShadowId { get; set; }
    public string ActivityTypeCode { get; set; } = string.Empty;
    public long WarehouseShadowId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public long? PicUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsRfba { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;
    public string? Notes { get; set; }
    public GpsCoordinate? GpsLocation { get; set; }
    public long CreatedByUserId { get; set; }
    public long? SubmittedByUserId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company Company { get; set; } = null!;
    public BudgetPlan BudgetPlan { get; set; } = null!;
    public BudgetPlanItem? BudgetPlanItem { get; set; }
    public ItemShadow Activity { get; set; } = null!;
    public WarehouseShadow Warehouse { get; set; } = null!;
    public User? PicUser { get; set; }
    public User CreatedBy { get; set; } = null!;
    public User? SubmittedBy { get; set; }

    public ICollection<WorkOrderTransportOrder> TransportOrders { get; set; } = [];
    public ICollection<WorkOrderUnloadingItem> UnloadingItems { get; set; } = [];
    public ICollection<WorkOrderLoadingItem> LoadingItems { get; set; } = [];
    public WorkOrderFumigationDetail? FumigationDetail { get; set; }
    public WorkOrderStorageDetail? StorageDetail { get; set; }
    public WorkOrderQcDetail? QcDetail { get; set; }
    public WorkOrderHeavyEquipDetail? HeavyEquipDetail { get; set; }
    public WorkOrderUnbaggingDetail? UnbaggingDetail { get; set; }
    public WorkOrderRebaggingDetail? RebaggingDetail { get; set; }
}
