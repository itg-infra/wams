namespace WAMS.Infrastructure.ExternalSync.Project;

/// <summary>
/// Maps the JSON response from GET /WAMS/LkProject. Field names confirmed live against
/// Entity=GCU (2026-07-21): {"bl":"SSZ1769067","prjCode":"MV.0000565","prjName":"CMA CGM ..."}.
/// </summary>
public record ProjectLookupDto(string? Bl, string? PrjCode, string? PrjName);
