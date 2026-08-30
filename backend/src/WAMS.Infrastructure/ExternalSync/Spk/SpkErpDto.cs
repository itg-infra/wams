namespace WAMS.Infrastructure.ExternalSync.Spk;

using System.Text.Json.Serialization;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkMOLOPMS?Company={code}
/// Field names match ERP response casing exactly.
/// </summary>
public record SpkErpDto(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("docNo")] string DocNo,
    [property: JsonPropertyName("baseDoc")] string BaseDoc,
    [property: JsonPropertyName("baseDocNo")] string BaseDocNo,
    [property: JsonPropertyName("cardCode")] string CardCode,
    [property: JsonPropertyName("cardName")] string CardName,
    [property: JsonPropertyName("itemCode")] string ItemCode,
    [property: JsonPropertyName("itemName")] string ItemName,
    [property: JsonPropertyName("quantity")] decimal? Quantity,
    [property: JsonPropertyName("deliveryQty")] decimal? DeliveryQty,
    [property: JsonPropertyName("uoM")] string UoM,
    [property: JsonPropertyName("packType")] string PackType,
    [property: JsonPropertyName("whsCode")] string WhsCode,
    [property: JsonPropertyName("whsName")] string WhsName,
    [property: JsonPropertyName("docStatus")] string DocStatus,
    [property: JsonPropertyName("blNo")] string? BlNo);
