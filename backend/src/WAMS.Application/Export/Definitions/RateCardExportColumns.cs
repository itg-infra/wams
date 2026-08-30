namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.RateCards;

public static class RateCardExportColumns
{
    public static List<ExportColumnDefinition<RateCardSummaryResponse>> Columns =>
    [
        new("Vendor Code", x => x.Vendor?.CardCode),
        new("Vendor Name", x => x.Vendor?.CardName),
        new("Status", x => x.Status),
        new("Item Count", x => x.ItemCount),
        new("Created At", x => x.CreatedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
