namespace WAMS.Infrastructure.ExternalSync.Pph;

/// <summary>
/// Maps the JSON response from GET /WAMS/PPh?Company={code}&amp;CardCode={cardCode}.
/// Field names must exactly match the ERP response casing (verified live: cardCode/cardName/wtCode/wtName/rate).
/// </summary>
public record PphErpDto(string CardCode, string CardName, string WtCode, string WtName, decimal Rate);
