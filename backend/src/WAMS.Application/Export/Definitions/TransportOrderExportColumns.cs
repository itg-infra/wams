namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.TransportOrders;

public static class TransportOrderExportColumns
{
    public static List<ExportColumnDefinition<TransportOrderShadowResponse>> Columns =>
    [
        new("Doc No", x => x.DocNo),
        new("Type", x => x.Type),
        new("Card Code", x => x.CardCode),
        new("Card Name", x => x.CardName),
        new("Vehicle No", x => x.VehicleNo),
        new("Vehicle Type", x => x.VehicleType),
        new("BL No", x => x.BlNo),
        new("Item Code", x => x.ItemCode),
        new("Item Name", x => x.ItemName),
        new("Quantity", x => x.Quantity, Format: "#,##0.###"),
        new("UoM", x => x.UoM),
        new("Warehouse Code", x => x.WhsCode),
        new("Warehouse Name", x => x.WhsName),
        new("Status", x => x.DocStatus),
    ];
}
