namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Items;

public static class ItemExportColumns
{
    public static List<ExportColumnDefinition<ItemSummaryResponse>> Columns =>
    [
        new("Item Code", x => x.ItemCode),
        new("Item Name", x => x.ItemName),
        new("Account Code", x => x.AcctCode),
        new("Account Name", x => x.AcctName),
    ];
}
