namespace WAMS.Infrastructure.ExternalSync.TransportOrder;

using System.Text.Json.Serialization;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkTOMOLOPMS?Company={code}
/// Field names match ERP response casing exactly.
/// </summary>
public record ToErpDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("docNo")] string DocNo,
    [property: JsonPropertyName("cardCode")] string CardCode,
    [property: JsonPropertyName("cardName")] string CardName,
    [property: JsonPropertyName("itemCode")] string ItemCode,
    [property: JsonPropertyName("itemName")] string ItemName,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("uoM")] string UoM,
    [property: JsonPropertyName("whsCode")] string WhsCode,
    [property: JsonPropertyName("whsName")] string WhsName,
    [property: JsonPropertyName("docStatus")] string DocStatus,
    [property: JsonPropertyName("blNo")] string BlNo,
    [property: JsonPropertyName("vehiclePlate")] string VehiclePlate,
    [property: JsonPropertyName("vehicleType")] string VehicleType);
