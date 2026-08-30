namespace WAMS.Infrastructure.ExternalSync.Ppn;

/// <summary>
/// Maps the JSON response from GET /WAMS/PPn?Company={code}.
/// Field names must exactly match the ERP response casing (verified live: pPnCode/pPnName/rate).
/// </summary>
public record PpnErpDto(string PpnCode, string PpnName, decimal Rate);
