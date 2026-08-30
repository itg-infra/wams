namespace WAMS.Infrastructure.ExternalSync.CostCenter;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkBranch, /WAMS/LkWarehouse, /WAMS/LkProduct, /WAMS/LkDivision
/// (all four "Cost Center" tagged endpoints share this shape). Field names must exactly match the ERP
/// response casing (verified live: ocrCode/ocrName, e.g. {"ocrCode":"3JKT","ocrName":"Jakarta"}).
/// </summary>
public record OcrLookupDto(string? OcrCode, string? OcrName);
