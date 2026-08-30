namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Spk;

public static class SpkExportColumns
{
    public static List<ExportColumnDefinition<SpkShadowResponse>> Columns =>
    [
        new("Doc No", x => x.DocNo),
        new("Type", x => x.Type),
        new("Base Doc", x => x.BaseDoc),
        new("Base Doc No", x => x.BaseDocNo),
        new("Card Code", x => x.CardCode),
        new("Card Name", x => x.CardName),
        new("Item Code", x => x.ItemCode),
        new("Item Name", x => x.ItemName),
        new("Quantity", x => x.Quantity, Format: "#,##0.###"),
        new("Delivery Qty", x => x.DeliveryQty, Format: "#,##0.###"),
        new("UoM", x => x.UoM),
        new("Pack Type", x => x.PackType),
        new("Warehouse Code", x => x.WhsCode),
        new("Warehouse Name", x => x.WhsName),
        new("BL No", x => x.BlNo),
        new("Status", x => x.DocStatus),
    ];
}
