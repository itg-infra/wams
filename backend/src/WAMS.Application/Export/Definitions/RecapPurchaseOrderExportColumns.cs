namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.PurchaseOrders;

public static class RecapPurchaseOrderExportColumns
{
    public static List<ExportColumnDefinition<ApprovedBudgetPlanPoStatusResponse>> Columns =>
    [
        new("Budget No", x => x.BudgetPlanCode),
        new("Vendor Name", x => x.VendorName),
        new("Total Budget", x => x.TotalBudgetPlan, Format: "#,##0.00"),
        new("Budget Approved", x => x.BudgetApproved, Format: "#,##0.00"),
        new("Budget Variance", x => x.BudgetVariance, Format: "#,##0.00"),
        new("Doc Date", x => x.DocDate, Format: "yyyy-MM-dd"),
        new("PO Numbers", x => string.Join(", ", x.PurchaseOrders.Select(po => po.Code))),
    ];
}
