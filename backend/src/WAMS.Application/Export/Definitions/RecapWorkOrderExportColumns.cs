namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.RecapWorkOrders;

public static class RecapWorkOrderExportColumns
{
    public static List<ExportColumnDefinition<RecapWorkOrderSummaryResponse>> Columns =>
    [
        new("Budget Plan", x => x.BudgetPlanCode),
        new("Template Code", x => x.TemplateCode),
        new("Warehouse", x => x.WarehouseName),
        new("Warehouse Code", x => x.WarehouseCode),
        new("Is RFBA", x => x.IsRfba),
        new("BL Numbers", x => x.BlNumbers),
        new("Activity Types", x => x.ActivityTypes),
        new("PIC Names", x => x.PicNames),
        new("Status", x => x.RecapStatus),
        new("Doc Date", x => x.DocDate, Format: "yyyy-MM-dd"),
        new("Remark", x => x.Remark),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
