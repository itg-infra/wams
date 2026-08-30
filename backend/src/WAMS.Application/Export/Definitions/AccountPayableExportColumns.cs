namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.AccountPayables;

public static class AccountPayableExportColumns
{
    public static List<ExportColumnDefinition<AccountPayableSummaryResponse>> Columns =>
    [
        new("Code", x => x.Code),
        new("Vendor Code", x => x.VendorCode),
        new("Vendor Name", x => x.VendorName),
        new("Status", x => x.Status),
        new("Doc Date", x => x.DocDate, Format: "yyyy-MM-dd"),
        new("SAP AP Number", x => x.SapApNumber),
        new("Grand Total", x => x.GrandTotal, Format: "#,##0.00"),
        new("Item Count", x => x.ItemCount),
        new("Remark", x => x.Remark),
        new("Created By", x => x.CreatedByName),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
