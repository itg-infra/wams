namespace WAMS.Application.Export.Definitions;

using WAMS.Application.DTOs.Warehouses;

public static class WarehouseExportColumns
{
    public static List<ExportColumnDefinition<WarehouseResponse>> Columns =>
    [
        new("Code", x => x.Code),
        new("Name", x => x.Name),
        new("Location", x => x.Location),
        new("Active", x => x.IsActive),
        new("First Seen At", x => x.FirstSeenAt, Format: "yyyy-MM-dd"),
        new("Synced At", x => x.SyncedAt, Format: "yyyy-MM-dd HH:mm"),
    ];
}
