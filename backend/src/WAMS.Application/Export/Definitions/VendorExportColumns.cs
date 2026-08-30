namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Vendors;

public static class VendorExportColumns
{
    public static List<ExportColumnDefinition<VendorSummaryResponse>> Columns =>
    [
        new("Card Code", x => x.CardCode),
        new("Card Name", x => x.CardName),
    ];
}
