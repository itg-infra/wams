namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.BudgetPlans;

public static class BudgetPlanExportColumns
{
    public static List<ExportColumnDefinition<BudgetPlanSummaryResponse>> Columns =>
    [
        new("Budget No", x => x.BudgetNo),
        new("Template Code", x => x.TemplateCode),
        new("Vendor", x => x.VendorName),
        new("Maker", x => x.MakerName),
        new("Location", x => x.Location),
        new("Status", x => x.StatusDisplay),
        new("Doc Date", x => x.DocDate, Format: "yyyy-MM-dd"),
        new("Remark", x => x.Remark),
    ];
}
