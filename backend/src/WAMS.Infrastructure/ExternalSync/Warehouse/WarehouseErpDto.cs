namespace WAMS.Infrastructure.ExternalSync.Warehouse;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkWhsCode?Company={code}
/// Field names must exactly match the ERP response casing.
/// </summary>
public record WarehouseErpDto(
    string WhsCode,
    string WhsName,
    string? Location
);