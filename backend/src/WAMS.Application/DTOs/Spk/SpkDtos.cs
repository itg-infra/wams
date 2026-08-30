namespace WAMS.Application.DTOs.Spk;

using WAMS.Application.Common;

public record SpkQuery : DataTableQuery
{
    public string? Type { get; init; }  // LO / MO
    public string? DocStatus { get; init; }  // O / C
    public string? WhsCode { get; init; }
}

public record SpkShadowResponse(
    long Id,
    string Type,
    string DocNo,
    string BaseDoc,
    string BaseDocNo,
    string CardCode,
    string CardName,
    string ItemCode,
    string ItemName,
    decimal? Quantity,
    decimal? DeliveryQty,
    string UoM,
    string PackType,
    string WhsCode,
    string WhsName,
    string DocStatus,
    string? BlNo);
