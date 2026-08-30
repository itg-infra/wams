namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.WorkOrders;

public static class WorkOrderExportColumns
{
    public static List<ExportColumnDefinition<WorkOrderSummaryResponse>> Columns =>
    [
        new("Code", x => x.Code),
        new("Budget Plan", x => x.BudgetPlanCode),
        new("Activity", x => x.ActivityName),
        new("Activity Type", x => x.ActivityTypeDisplay),
        new("Warehouse", x => x.WarehouseName),
        new("Warehouse Code", x => x.WarehouseCode),
        new("Status", x => x.Status),
        new("Is RFBA", x => x.IsRfba),
        new("Start Date", x => x.StartDate, Format: "yyyy-MM-dd"),
        new("End Date", x => x.EndDate, Format: "yyyy-MM-dd"),
        new("PIC", x => x.PicName),
        new("BL Number", x => x.BlNumber),
        new("Product", x => x.ProductName),
        new("Vessel", x => x.VesselName),
        new("Created By", x => x.CreatedByName),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
