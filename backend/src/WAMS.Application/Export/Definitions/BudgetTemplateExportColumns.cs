namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.BudgetTemplates;

public static class BudgetTemplateExportColumns
{
    public static List<ExportColumnDefinition<BudgetTemplateSummaryResponse>> Columns =>
    [
        new("Template Code", x => x.TemplateCode),
        new("Location", x => x.ProvinceDisplay),
        new("Status", x => x.Status),
        new("Date", x => x.Date, Format: "yyyy-MM-dd"),
    ];
}
